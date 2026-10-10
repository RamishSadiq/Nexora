# US and Middle East five-hour campaign update

10 October 2026. User clarified that US outreach was not stopped and requested both markets together every five hours.

Updated the existing nexora-daily-us-sales-outreach automation in place, named Nexora US and Middle East outreach every five hours. Saved status ACTIVE; the original target chat and notification preference were preserved. The current prompt explicitly covers both markets. Shared state and current policy use five-hour cadence, one new prospect per market and two total per interval; daily maximum remains 24 including manual sends. The separate reply monitor is unchanged.

Preserved all 55 previously contacted organisations, historical hourly runs, manual batch records, positive-response stops and suppressions. The three UAE messages sent earlier today remain confirmed and excluded from new first pitches. No extra email was sent while updating the schedule.

Validation: re-read saved automation/state; verified active status, five-hour recurrence, both markets and target identity. Six isolated helper checks passed: empty interval allows US, recent US blocks another US, recent US allows Middle East, two recent sends block another, an expired six-hour contact does not block the current interval, and recording a confirmed send preserves its original timestamp. Fixture checks did not use Gmail or modify live campaign contacts. Temporary fixtures were confined to Nexora artifacts and cleaned up.

Current authority: [DUAL-MARKET-CAMPAIGN.md](../DUAL-MARKET-CAMPAIGN.md). Recipient/source qualification, applicable contact restrictions, live reply access and the desktop runtime still determine whether a given scheduled run can send. Both markets are processed in the same run; Gmail calls are serial.
