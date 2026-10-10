// Each integration test may host an API and its own database. Serial execution
// keeps the full regression usable alongside the three local frontend processes.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
