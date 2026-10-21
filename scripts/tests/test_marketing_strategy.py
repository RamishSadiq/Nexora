"""Offline regressions for new campaign quality holds; no mail or real-state writes."""
import copy
from datetime import datetime, timezone
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SCRIPTS = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('strategy', SCRIPTS / 'marketing_strategy.py')
strategy = importlib.util.module_from_spec(spec)
spec.loader.exec_module(strategy)
NOW = datetime(2026, 10, 21, 8, tzinfo=timezone.utc)


class StrategyTests(unittest.TestCase):
    def setUp(self):
        self.policy = strategy.read(SCRIPTS.parent / strategy.POLICY)
        self.policy['deliveryReadiness'].update(status='cleared', seedChecks=[
            {'provider': provider, 'placement': 'inbox', 'campaignEquivalent': True,
             'sender': self.policy['sender'], 'messageId': provider,
             'authenticationPassed': True, 'checkedAt': NOW.isoformat()}
            for provider in ['gmail', 'outlook']])
        self.policy['deliveryReadiness']['incidentReview'] = {'decision': 'cleared_with_limits', 'evidencePath': 'controlled-test-review.json', 'checkedAt': NOW.isoformat()}
        self.candidate = {'market': 'United States', 'offerType': 'membership',
                          'crmStatus': 'manual_records_evidence', 'strategicEvidence': {
            'cohortId': 'membership-manual-applications-v1',
            'copyVersion': 'membership-problem-evaluation-noattachment-v1',
            'ctaVariant': 'applicable_first_system_question',
            'offerHypothesis': 'Test one supported application review journey',
            'signal': {'kind': 'documented_current_manual_workflow',
                       'currentOfficialPublication': True, 'url': 'https://example.org/apply',
                       'checkedAt': NOW.isoformat(), 'detail': 'Send completed application to membership staff',
                       'workflowRelevance': 'Applicant and staff review'},
            'buyer': {'role': 'Membership administrator', 'url': 'https://example.org/team',
                      'contactPurpose': 'Membership administration', 'offerRelevance': 'Application review',
                      'publishedRoleRelevant': True, 'checkedAt': NOW.isoformat()},
            'proof': {'assetId': 'membership_intro'}}}

    def assess(self, **kwargs):
        return strategy.assess_candidate(self.candidate, self.policy, NOW, **kwargs)

    def test_strong_evidence_passes_strategy_but_not_live_send_authority(self):
        result = self.assess()
        self.assertTrue(result['strategyEligible'])
        self.assertTrue(result['notSendAuthorisation'])

    def test_spam_observation_blocks_even_with_authentication_pass(self):
        self.policy['deliveryReadiness']['status'] = 'hold_seed_spam_observed'
        self.assertIn('delivery_readiness_hold', self.assess()['reasons'])

    def test_duplicate_provider_or_expired_test_does_not_clear_delivery(self):
        self.policy['minimumDistinctSeedProviders'] = 2
        self.policy['deliveryReadiness']['seedChecks'][1]['provider'] = 'gmail'
        self.assertFalse(self.assess()['strategyEligible'])
        self.policy['deliveryReadiness']['seedChecks'][1].update(provider='outlook', checkedAt='2026-10-01T08:00:00Z')
        self.assertFalse(self.assess()['strategyEligible'])

    def test_existing_website_or_future_change_is_not_current_need(self):
        self.candidate['strategicEvidence']['signal']['kind'] = 'website_has_project_pages'
        self.assertFalse(self.assess()['strategyEligible'])
        self.candidate['strategicEvidence']['signal'].update(kind='specific_supported_workflow_project', date='2026-11-01')
        self.assertIn('change_signal_is_future_or_stale', self.assess()['reasons'])

    def test_membership_video_is_not_cms_proof(self):
        self.candidate.update(offerType='website_cms')
        self.candidate['strategicEvidence'].update(cohortId='cms-active-content-project-v1', copyVersion='cms-project-evaluation-noattachment-v1', ctaVariant='relevance_before_meeting')
        self.assertIn('verified_offer_matched_proof_required', self.assess()['reasons'])

    def test_generic_inbox_and_unknown_crm_do_not_gain_clearance(self):
        self.candidate['strategicEvidence']['buyer']['publishedRoleRelevant'] = False
        self.candidate['crmStatus'] = 'unknown'
        reasons = self.assess()['reasons']
        self.assertIn('general_sales_or_enquiries_inbox_is_not_buyer_evidence', reasons)
        self.assertIn('existing_first_system_qualification_still_required', reasons)

    def test_content_version_churn_and_learning_batch_are_blocked(self):
        self.candidate['strategicEvidence']['copyVersion'] = 'new-version-today'
        self.assertIn('stable_registered_copy_version_required', self.assess()['reasons'])
        self.candidate['strategicEvidence']['copyVersion'] = 'membership-problem-evaluation-noattachment-v1'
        history = [{**copy.deepcopy(self.candidate), 'messageId': str(i)} for i in range(15)]
        history.append(history[0])
        self.assertIn('cohort_learning_batch_full_review_before_more_sends', self.assess(cohort_history=history)['reasons'])

    def test_weekday_age_excludes_current_partial_day_and_weekend(self):
        self.assertEqual(strategy.full_weekdays('2026-10-19T09:00:00Z', NOW), 1)
        self.assertEqual(strategy.full_weekdays('2026-10-16T09:00:00Z', NOW), 2)

    def test_unknown_history_is_not_an_attributed_failed_cohort(self):
        with tempfile.TemporaryDirectory() as name:
            root = Path(name)
            files = {
                'docs/market/us/daily/STATE.json': {'previouslyContacted': [{'messageId': str(i)} for i in range(16)], 'positiveResponses': []},
                'docs/market/acquisition/PIPELINE.json': {'opportunities': []},
                'docs/market/us/daily/ONE-TIME-FOLLOWUPS.json': {'entries': []},
                'docs/market/us/outreach/2026-10-01/campaign.json': {'messages': [{'messageId': str(i), 'status': 'sent', 'sentAt': '2026-10-01T08:00:00Z'} for i in range(16)]}}
            for rel, value in files.items():
                path = root / rel
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text(json.dumps(value))
            result = strategy.audit(root, NOW)
            self.assertEqual(result['uniqueFirstPitches'], 16)
            self.assertEqual(result['unattributedOfferSends'], 16)
            self.assertFalse(result['cohorts'][0]['reviewRequired'])
            self.assertTrue(result['cohorts'][0]['missingAttributionReviewRequired'])

    def test_recorder_hold_writes_nothing_and_accepted_id_still_reconciles(self):
        with tempfile.TemporaryDirectory() as name:
            root = Path(name)
            scripts = root / 'scripts'
            scripts.mkdir()
            for file in ['Record-USMarketOutreach.py', 'marketing_strategy.py']:
                (scripts / file).write_bytes((SCRIPTS / file).read_bytes())
            market = root / 'docs/market'
            (market / 'acquisition').mkdir(parents=True)
            (market / 'acquisition/STRATEGY.json').write_text(json.dumps(self.policy | {'deliveryReadiness': {'status': 'hold', 'seedChecks': []}}))
            (market / 'us/daily').mkdir(parents=True)
            state = {'campaignStatus': 'active', 'previouslyContacted': [], 'runs': []}
            state_path = market / 'us/daily/STATE.json'
            state_path.write_text(json.dumps(state))
            path = market / 'us/outreach/2026-10-21/campaign.json'
            path.parent.mkdir(parents=True)
            message = self.candidate | {'to': 'member@example.org', 'organisation': 'Example', 'status': 'ready'}
            path.write_text(json.dumps({'dailyCampaign': True, 'localDate': '2026-10-21', 'messages': [message]}))
            before = path.read_bytes(), state_path.read_bytes()
            cmd = [sys.executable, str(scripts / 'Record-USMarketOutreach.py'), '--campaign', str(path), '--recipient', message['to']]
            blocked = subprocess.run(cmd + ['--status', 'sending'], capture_output=True, text=True)
            self.assertNotEqual(blocked.returncode, 0)
            self.assertEqual(before, (path.read_bytes(), state_path.read_bytes()))
            accepted = subprocess.run(cmd + ['--status', 'sent', '--message-id', 'accepted-1', '--thread-id', 'accepted-1'], capture_output=True, text=True)
            self.assertEqual(accepted.returncode, 0, accepted.stderr)
            first_sent_at = strategy.read(path)['messages'][0]['sentAt']
            repeated = subprocess.run(cmd + ['--status', 'sent', '--message-id', 'accepted-1', '--thread-id', 'accepted-1'], capture_output=True, text=True)
            self.assertEqual(repeated.returncode, 0, repeated.stderr)
            self.assertEqual(strategy.read(path)['messages'][0]['sentAt'], first_sent_at)
            self.assertEqual(len(strategy.read(state_path)['previouslyContacted']), 1)


if __name__ == '__main__':
    unittest.main()
