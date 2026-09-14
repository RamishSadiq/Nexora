# Stage 5: Events, learning and examinations

Delivered: event/course/exam schedules, sessions, Contact bookings, course applications and approval/rejection, capacity and waitlists, promotion, transfers, attendance, cancellation, immutable exam scores/outcomes and actor/reason history. All appear in Events and learning.

A versioned parent offering serializes capacity changes, including transfers touching both source and destination. Composite tenant foreign keys and unique participant/offering and result/booking constraints prevent foreign references and duplicate bookings/results. Courses require approval before attendance; exam results require attended bookings. These are staff-entered operational records; no candidate is automatically admitted or notified externally.

Permissions are events.read/manage/results. InitialEvents migration applied to isolated SQL Server review database. Integration tests cover capacity/waitlisting, promotion, transfer, attendance, stale writes, course approval, exam pass/fail, duplicate result prevention and foreign contact rejection. Frontend lint/build pass. Full exam moderation/amendment policy, session-level attendance and production accessibility/load acceptance remain release follow-ups.
