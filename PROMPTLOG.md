# AI Prompt Log (Task 5)

## Prompt 1 — Task 1 (API Client Debugging)  
**Tool:** Perplexity AI
**Prompt:** "Debug JsonSerializer compile error and 400 Bad Request from MockApi order creation"
**Outcome:** Suggested three fixes. **Kept** raw `StringContent` to preserve exact payload structure from legacy tests (backward compatibility), **changed** `Guid? Id` → `Guid Id` (fixed model binding), **rejected** `PostAsJsonAsync` due to serialization differences breaking MockApi expectations. After 3 iterations, `CreateOrder_ReturnsSuccess` passed.

## Prompt 2 — Task 2 (Builder Pattern)
**Tool:** Perplexity AI
**Prompt:** "Extract 8 hardcoded test emails from OrderTests.cs into fluent OrderBuilder method for DRY compliance"
**Outcome:** Generated `WithScenarioEmail("scenario")` mapping "sql-injection" → "sql-injection@example.com". **Enhanced** with 8 real scenarios (unicode/café emojis, performance edge cases). **Rejected** constants class - fluent API provides better discoverability and readability.

## Prompt 3 — Task 3 (Polling Utility)  ← **FIXED**
**Tool:** Perplexity AI
**Prompt:** "Generic async polling utility to replace Thread.Sleep() with configurable timeout and last state diagnostics"  
**Outcome:** Generated strong baseline `WaitUntilAsync<T>` meeting core requirements (500ms interval, 15s timeout, generic typing). **Validated** async safety and rejection of Thread.Sleep retries (would reintroduce flakiness). **Added** JSON serialization of last observed state for timeout diagnostics. Status transition test now completes in ~5.2s vs fixed 6s.

## Prompt 4 — Task 4 (Security Testing)
**Tool:** Perplexity AI
**Prompt:** "Generate SQL injection payloads for x-kibo-tenant header following OWASP guidelines with production risk assessment"
**Outcome:** Suggested 3 payloads. Used classic `"tenant1'; DROP TABLE Orders; --"` which returned **201 Created** (CRITICAL). Added comprehensive XML docblock documenting security bug with Risk/Impact/Recommendation.

## Prompt 5 — Task 6 (Observability)
**Tool:** Perplexity AI
**Prompt:** "HttpClient DelegatingHandler pattern for correlation ID, timing, toggleable logging to response headers"
**Outcome:** Generated a solid `ObservabilityHandler` implementation. **Evaluated** for separation of concerns and test fixture toggleability. **Added** constructor `enableLogging` parameter for environment-dependent behavior. Headers extract perfectly via `ObservabilityHelpers`.

## Prompt 6 — Task 4 (Test Documentation)
**Tool:** Perplexity AI
**Prompt:** "Move inline BUG REPORT comments to XML docblocks and add Expected/Actual behavior to all edge case tests"  
**Outcome:** First pass - lacked consistent formatting across all tests. Second pass - refined prompt to enforce uniform docblock structure. Moved 4 inline `// BUG REPORT` comments to XML docblocks (clean code principle). SQLi report positioning already optimal, kept untouched.