# Contact Social Media Details

4 October 2026. Implements the eleven CONTACT Social Media Details fields in BW2:CG2, with optional metric provenance.

| Workbook field | Staff API field | Storage |
| --- | --- | --- |
| Facebook_ID | facebookId | ContactSocialProfile, channel=facebook |
| X_ID | xId | ContactSocialProfile, channel=x |
| LinkedIn ID | linkedInId | ContactSocialProfile, channel=linkedin |
| YouTube_ID | youTubeId | ContactSocialProfile, channel=youtube |
| Online_Communities | onlineCommunities | ContactSocialProfile, channel=online-communities |
| Google_ID | googleId | ContactSocialProfile, channel=google |
| Number_Of_Followers | numberOfFollowers | ContactProfile.NumberOfFollowers |
| Number_Of_Following | numberOfFollowing | ContactProfile.NumberOfFollowing |
| Number_Of_Tweets_Sent | numberOfTweetsSent | ContactProfile.NumberOfTweetsSent |
| Number_Of_Mentions | numberOfMentions | ContactProfile.NumberOfMentions |
| Number_Of_Tweets_With_Tags | numberOfTweetsWithTags | ContactProfile.NumberOfTweetsWithTags |

`metricsObservedOn` and `metricsSource` additionally capture the date and source of these recorded count snapshots. They are optional and are not inferred from migration samples. The template does not identify per-platform ownership for the five counts, so this slice preserves their Contact-level meaning. Online Communities retains the supplied text without splitting it into invented associations.

## Behaviour and validation

Social Media Details appears between General Information and Communication Permissions in the Contact form. Staff can enter identifiers or profile URLs; the UI displays them as escaped text. Identifiers are trimmed, allow up to 320 characters without control characters and become unset when blank. Each Contact has at most one stored identifier for each channel, enforced by a unique tenant/record/channel index. Blank identifiers remove the corresponding related row.

Counts are optional non-negative whole numbers up to JavaScript's maximum safe integer, 9007199254740991. Zero is retained independently of unset. They use SQL bigint storage, including values above the 32-bit integer limit. The request contract accepts numeric values for explicit whole-number validation, returning a validation response for fractions rather than failing during integer binding. Observation dates must be valid calendar dates on or before the current UTC date; no timezone conversion applies. Source is trimmed and limited to 160 characters without control characters.

The existing staff detail response returns `socialMediaDetails`. The version-checked profile update accepts that optional object alongside Contact names. Omission/null preserves existing social values; a supplied empty object clears identifiers, metrics and provenance. Name changes from older staff clients or Portal preserve these values. Staff permissions, tenant boundaries, Contact-only parent validation, immutable parent links, archive checks, activity and data audit apply. Account updates reject this Contact-specific object. Portal profile contracts and public directory publication remain outside this field slice. Social network connections, automatic counter refresh and workbook import are separate future work.

## Migration and next scope

`20261004082429_ContactSocialMediaDetails` creates the tenant-bound ContactSocialProfiles table with its parent foreign key and unique channel index. It adds five nullable counters and two nullable provenance properties to ContactProfiles, preserving existing profiles and permissions. Apply it after Contact Communication Permissions through the normal migration procedure before using the new API against an existing database. Isolated tests do not migrate the shared development database.

## Verification

Three API checks passed: the Social Media Details round-trip, validation, clearing and staff-only access check, plus existing structured-name and communication-permission regressions. One isolated SQL Server migration test passed, covering upgrade preservation, repeat migration, large and zero counts, channel uniqueness, tenant isolation and Contact parent integrity. No tests were skipped.

TypeScript, focused ESLint and the production frontend build passed. The build retained the seven existing generated CSS warnings. Authenticated browser appearance remains unverified because the preceding browser check encountered unavailable local sign-in. This task has not applied the migration to the shared development database; source changes remain uncommitted.

[Managed Profile taxonomy and assignments](CONTACT_PROFILE_TAXONOMY.md) is now implemented as the following slice. Restricted EDI and directory publication retain the documented future-scope boundaries.
