param([switch]$IncludeChapters, [string[]]$DocumentStems)
$ErrorActionPreference = 'Stop'
$guideRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$guideOutput = Join-Path $guideRoot 'artifacts\platform-guide-qa\pdf'
New-Item -ItemType Directory -Path $guideOutput -Force | Out-Null
$guideDocuments = @((Join-Path $guideRoot 'docs\beginner-guides\Nexora complete beginner guide.docx'))
if ($IncludeChapters) {
    $guideDocuments += @(Get-ChildItem -LiteralPath (Join-Path $guideRoot 'docs\beginner-guides\chapters') -Filter '*.docx' | Sort-Object Name | Select-Object -ExpandProperty FullName)
}
if ($DocumentStems) {
    $guideDocuments = @($guideDocuments | Where-Object { [IO.Path]::GetFileNameWithoutExtension($_) -in $DocumentStems })
    if ($guideDocuments.Count -ne $DocumentStems.Count) { throw 'A requested documentation file was not found in the selected guide set.' }
}
$guideWord = New-Object -ComObject Word.Application
$guideWord.Visible = $false
$guideWord.DisplayAlerts = 0
try {
    foreach ($guideDocumentPath in $guideDocuments) {
        $guideDocument = $null
        try {
            $guideDocument = $guideWord.Documents.Open($guideDocumentPath, $false, $true)
            $guidePdfPath = Join-Path $guideOutput (([IO.Path]::GetFileNameWithoutExtension($guideDocumentPath)) + '.pdf')
            $guideDocument.ExportAsFixedFormat($guidePdfPath, 17)
            $guidePageCount = $guideDocument.ComputeStatistics(2)
            [pscustomobject]@{ Document = [IO.Path]::GetFileName($guideDocumentPath); Pages = $guidePageCount }
        } finally {
            if ($null -ne $guideDocument) {
                $guideDocument.Close(0)
                [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($guideDocument)
            }
        }
    }
} finally {
    $guideWord.Quit()
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($guideWord)
}
