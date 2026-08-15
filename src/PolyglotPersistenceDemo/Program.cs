using System.Diagnostics;
using PolyglotPersistenceDemo.Services;
using PolyglotPersistenceDemo.Stores;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("================================================================================");
Console.WriteLine("    POLYGLOT PERSISTENCE & EVENT-DRIVEN DENORMALIZATION BENCHMARK HARNESS      ");
Console.WriteLine("================================================================================");
Console.ResetColor();

var sqlStore = new MockSqlBillingStore();
var cosmosStore = new MockCosmosTelemetryStore();

var antiPatternService = new AntiPatternDistributedJoinService(sqlStore, cosmosStore);
var denormalizedService = new DenormalizedPartitionQueryService(cosmosStore);
var denormalizer = new EventDrivenDenormalizer(sqlStore, cosmosStore);

const int UserCohortSize = 30;
var targetUserIds = Enumerable.Range(1, UserCohortSize).Select(i => $"user_{i:D3}").ToList();

Console.WriteLine($"\n[INFO] Initializing benchmark for cohort of {UserCohortSize} concurrent user sessions...\n");

// -----------------------------------------------------------------------------
// BENCHMARK 1: Cross-Boundary Relational Join Anti-Pattern (O(N))
// -----------------------------------------------------------------------------
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("--------------------------------------------------------------------------------");
Console.WriteLine("  1. EXECUTING ANTI-PATTERN: Cross-Database Distributed Application Join (O(N)) ");
Console.WriteLine("--------------------------------------------------------------------------------");
Console.ResetColor();

sqlStore.ResetQueryCounter();
cosmosStore.ResetMetrics();

var (antiPatternResults, antiPatternElapsedMs, antiPatternQueryCount) =
    await antiPatternService.ExecuteDistributedJoinAsync(UserCohortSize);

var antiPatternSqlQueries = sqlStore.TotalQueriesExecuted;
var antiPatternCosmosPointReads = cosmosStore.TotalPointReads;
var antiPatternTotalRUs = cosmosStore.TotalConsumedRUs;

Console.ForegroundColor = ConsoleColor.Red;
Console.WriteLine($"[ANTI-PATTERN RESULT] Processed {antiPatternResults.Count} profiles in {antiPatternElapsedMs} ms");
Console.WriteLine($"  - Total Database Network Roundtrips: {antiPatternQueryCount} (SQL: {antiPatternSqlQueries}, Cosmos: {antiPatternCosmosPointReads})");
Console.WriteLine($"  - Cosmos RU Cost: {antiPatternTotalRUs:F1} RUs");
Console.ResetColor();

// -----------------------------------------------------------------------------
// BENCHMARK 2: Denormalized Partition Query Solution (O(1))
// -----------------------------------------------------------------------------
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("\n--------------------------------------------------------------------------------");
Console.WriteLine("  2. EXECUTING SOLUTION: Denormalized Partition Document Queries (O(1))         ");
Console.WriteLine("--------------------------------------------------------------------------------");
Console.ResetColor();

sqlStore.ResetQueryCounter();
cosmosStore.ResetMetrics();

var (denormalizedResults, denormalizedElapsedMs, denormalizedQueryCount) =
    await denormalizedService.ExecuteSinglePartitionQueriesAsync(targetUserIds);

var denormalizedSqlQueries = sqlStore.TotalQueriesExecuted;
var denormalizedCosmosPointReads = cosmosStore.TotalPointReads;
var denormalizedTotalRUs = cosmosStore.TotalConsumedRUs;

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"[DENORMALIZED RESULT] Processed {denormalizedResults.Count} profiles in {denormalizedElapsedMs} ms");
Console.WriteLine($"  - Total Database Network Roundtrips: {denormalizedQueryCount} (SQL: {denormalizedSqlQueries}, Cosmos: {denormalizedCosmosPointReads})");
Console.WriteLine($"  - Cosmos RU Cost: {denormalizedTotalRUs:F1} RUs");
Console.ResetColor();

// -----------------------------------------------------------------------------
// DEMO 3: Event-Driven Metadata Synchronization
// -----------------------------------------------------------------------------
Console.ForegroundColor = ConsoleColor.Magenta;
Console.WriteLine("\n--------------------------------------------------------------------------------");
Console.WriteLine("  3. SIMULATING EVENT-DRIVEN SUBSCRIPTION UPGRADE & IMMEDIATE O(1) READ         ");
Console.WriteLine("--------------------------------------------------------------------------------");
Console.ResetColor();

var targetUser = "user_001";
Console.WriteLine($"[EVENT] User '{targetUser}' upgrades subscription to 4K Ultra HDR in SQL Billing...");
var upgradeEvent = await denormalizer.UpgradeSubscriptionAsync(targetUser, "plan-premium-4k");
Console.WriteLine($"[EVENT DISPATCHED] EventId: {upgradeEvent.EventId}, NewTier: {upgradeEvent.NewTier}, MaxStreams: {upgradeEvent.NewMaxStreams}");

// Verify immediate read from Cosmos
var updatedProfile = await cosmosStore.GetDenormalizedUserProfileAsync(targetUser);
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine($"[STREAMING READ] Read Denormalized Profile for '{targetUser}' from Cosmos DB:");
Console.WriteLine($"  - Tier:           {updatedProfile?.Tier}");
Console.WriteLine($"  - Max Streams:    {updatedProfile?.MaxStreams}");
Console.WriteLine($"  - Max Resolution: {updatedProfile?.MaxResolution}");
Console.WriteLine($"  - Sync Timestamp: {updatedProfile?.LastSynchronizedAt:yyyy-MM-dd HH:mm:ss.fff}");
Console.ResetColor();

// -----------------------------------------------------------------------------
// SIDE-BY-SIDE BENCHMARK COMPARISON TABLE
// -----------------------------------------------------------------------------
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("\n====================================================================================================");
Console.WriteLine("                                  SIDE-BY-SIDE BENCHMARK SUMMARY                                    ");
Console.WriteLine("====================================================================================================");
Console.ResetColor();

var speedup = (double)antiPatternElapsedMs / Math.Max(1, denormalizedElapsedMs);

Console.WriteLine($"| {"Architecture Pattern",-38} | {"Complexity",-10} | {"Latency (ms)",-12} | {"Total DB Hops",-14} | {"Cosmos RUs",-12} |");
Console.WriteLine($"|{new string('-', 40)}|{new string('-', 12)}|{new string('-', 14)}|{new string('-', 16)}|{new string('-', 14)}|");
Console.WriteLine($"| {"Distributed In-Memory Join (Anti-Pattern)",-38} | {"O(N)",-10} | {$"{antiPatternElapsedMs} ms",-12} | {$"{antiPatternQueryCount} hops",-14} | {$"{antiPatternTotalRUs:F1} RUs",-12} |");
Console.WriteLine($"| {"Event-Driven Denormalization (Solution)",-38} | {"O(1)",-10} | {$"{denormalizedElapsedMs} ms",-12} | {$"{denormalizedQueryCount} hops",-14} | {$"{denormalizedTotalRUs:F1} RUs",-12} |");
Console.WriteLine($"|{new string('-', 40)}|{new string('-', 12)}|{new string('-', 14)}|{new string('-', 16)}|{new string('-', 14)}|");

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\n[BENCHMARK VERDICT] Event-Driven Denormalization achieved a {speedup:F1}x Speedup and reduced network hops by {antiPatternQueryCount - denormalizedQueryCount} queries!");
Console.ResetColor();

Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("\nKey Architectural Takeaways:");
Console.WriteLine("  1. Never perform distributed JOINs across ACID and NoSQL persistence boundaries.");
Console.WriteLine("  2. Leverage asynchronous domain events to project denormalized read-models into NoSQL stores.");
Console.WriteLine("  3. Co-locate all streaming session requirements in a single Cosmos document partitioned by /userId.");
Console.ResetColor();
