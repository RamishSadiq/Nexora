using Nexora.BuildingBlocks.Domain;
namespace Nexora.Modules.Finance.Domain;
public sealed class CatalogueProduct:TenantEntity
{
 public string Sku{get;set;}="";public string Name{get;set;}="";public decimal UnitPrice{get;set;}public string Currency{get;set;}="GBP";public int VatBasisPoints{get;set;}public bool IsActive{get;set;}=true;
}
public sealed class SalesOrder:TenantEntity
{
 public Guid CustomerId{get;set;}public string CustomerName{get;set;}="";public string Currency{get;set;}="GBP";public string Status{get;set;}="draft";public decimal Net{get;set;}public decimal Tax{get;set;}public decimal Total{get;set;}
}
public sealed class OrderLine:TenantEntity,IAppendOnly
{
 public Guid OrderId{get;set;}public Guid ProductId{get;set;}public string Description{get;set;}="";public int Quantity{get;set;}public decimal UnitPrice{get;set;}public int VatBasisPoints{get;set;}public decimal Net{get;set;}public decimal Tax{get;set;}
}
public sealed class PostedInvoice:TenantEntity
{
 public Guid OrderId{get;set;}public Guid CustomerId{get;set;}public string CustomerName{get;set;}="";public string Number{get;set;}="";public string Currency{get;set;}="GBP";public decimal Total{get;set;}
}
public sealed class Receipt:TenantEntity
{
 public Guid CustomerId{get;set;}public string Reference{get;set;}="";public decimal Amount{get;set;}public string Currency{get;set;}="GBP";public string? StatementReference{get;set;}
}
public sealed class Credit:TenantEntity,IAppendOnly
{
 public Guid InvoiceId{get;set;}public string Reference{get;set;}="";public string Reason{get;set;}="";public decimal Amount{get;set;}
}
public sealed class Allocation:TenantEntity,IAppendOnly
{
 public Guid ReceiptId{get;set;}public Guid InvoiceId{get;set;}public decimal Amount{get;set;}public Guid? ReversesId{get;set;}
}
public sealed class Refund:TenantEntity,IAppendOnly
{
 public Guid ReceiptId{get;set;}public decimal Amount{get;set;}public string Reference{get;set;}="";public string Reason{get;set;}="";
}
public sealed class FinanceHistory:TenantEntity,IAppendOnly
{
 public Guid SubjectId{get;set;}public Guid ActorUserId{get;set;}public string Action{get;set;}="";public string Reason{get;set;}="";public string CorrelationId{get;set;}="";
}
