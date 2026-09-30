# Migration template field inventory

29 September 2026. Extracted read-only from `Data Migration Template.xlsx` on the user's Desktop. Source SHA-256: `1b7a826036c751570c9f6200851731d68315616925715e003b4f73807f570694`.

22 sheets and 717 non-empty field-header occurrences. Repeated field names across sections/sheets are counted separately; this is not 717 distinct database columns. Blank formatted rows/columns are excluded. Exact sheet names and header spelling are retained for import mapping. Red headings are recorded as formatting evidence, not confirmed mandatory-field rules. No sample personal or banking values are copied.

See [findings and schema recommendations](DATA_MIGRATION_FIELD_REVIEW.md).

## CONTACT

Exact source sheet name: `CONTACT`. Header row 2. 185 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| B2 | Person_ID | General Information | Yes |
| C2 | Title | General Information |  |
| D2 | First_Name | General Information | Yes |
| E2 | Middle_Name | General Information |  |
| F2 | Last_Name | General Information |  |
| G2 | Previous_Last_Name | General Information |  |
| H2 | Post_Nominal | General Information |  |
| I2 | Contact_Type | General Information |  |
| J2 | Contact_Status | General Information |  |
| K2 | Main_Account (ID) | General Information |  |
| L2 | Department | General Information |  |
| M2 | Job_Title | General Information |  |
| N2 | Nationality | General Information |  |
| O2 | Old_Reference | General Information |  |
| P2 | Region | General Information |  |
| Q2 | Branch | General Information |  |
| R2 | Remarks | General Information |  |
| S2 | Primary_Email | Emails | Yes |
| T2 | Secondary_Email | Emails |  |
| U2 | Other_Email_1 | Emails |  |
| V2 | Other_Email_2 | Emails |  |
| W2 | Other_Email_3 | Emails |  |
| X2 | Other_Email_4 | Emails |  |
| Y2 | Other_Email_5 | Emails |  |
| Z2 | Mobile | Mobile,Telephone and Fax |  |
| AA2 | Primary_Telephone | Mobile,Telephone and Fax |  |
| AB2 | Secondary_Telephone | Mobile,Telephone and Fax |  |
| AC2 | Other_Telephone_1 | Mobile,Telephone and Fax |  |
| AD2 | Other_Telephone_2 | Mobile,Telephone and Fax |  |
| AE2 | Other_Telephone_3 | Mobile,Telephone and Fax |  |
| AF2 | Other_Telephone_4 | Mobile,Telephone and Fax |  |
| AG2 | Other_Telephone_5 | Mobile,Telephone and Fax |  |
| AH2 | Fax | Mobile,Telephone and Fax |  |
| AI2 | Mailing_Address_Type | Addresses |  |
| AJ2 | Mailing_Address_Account Name | Addresses |  |
| AK2 | Mailing_Address_Job_Title | Addresses |  |
| AL2 | Mailing_Address_Postcode | Addresses |  |
| AM2 | Mailing_Address_Line_1 | Addresses |  |
| AN2 | Mailing_Address_Line_2 | Addresses |  |
| AO2 | Mailing_Address_Line_3 | Addresses |  |
| AP2 | Mailing_Address_Line_4 | Addresses |  |
| AQ2 | Mailing_Address_Town | Addresses |  |
| AR2 | Mailing_Address_County | Addresses |  |
| AS2 | Mailing_Address_Country | Addresses |  |
| AT2 | Mailing_And_Billing_Address_Are_Same (Yes/No) | Addresses |  |
| AU2 | Billing_Address_Type | Addresses |  |
| AV2 | Billing_Address_Account_Name | Addresses |  |
| AW2 | Billing_Address_Job_Title | Addresses |  |
| AX2 | Billing_Address_Postcode | Addresses |  |
| AY2 | Billing_Address_Line_1 | Addresses |  |
| AZ2 | Billing_Address_Line_2 | Addresses |  |
| BA2 | Billing_Address_Line_3 | Addresses |  |
| BB2 | Billing_Address_Line_4 | Addresses |  |
| BC2 | Billing_Address_Town | Addresses |  |
| BD2 | Billing_Address_County | Addresses |  |
| BE2 | Billing_Address_Country | Addresses |  |
| BF2 | Other_Address_Postcode | Addresses |  |
| BG2 | Other_Address_Line_1 | Addresses |  |
| BH2 | Other_Address_Line_2 | Addresses |  |
| BI2 | Other_Address_Line_3 | Addresses |  |
| BJ2 | Other_Address_Line_4 | Addresses |  |
| BK2 | Other_Address_Town | Addresses |  |
| BL2 | Other_Address_County | Addresses |  |
| BM2 | Other_Address_Country | Addresses |  |
| BN2 | Other_2_Address Postcode | Addresses |  |
| BO2 | Other_2_Address_Line_1 | Addresses |  |
| BP2 | Other_2_Address_Line_2 | Addresses |  |
| BQ2 | Other_2_Address_Line_3 | Addresses |  |
| BR2 | Other_2_Address_Line_4 | Addresses |  |
| BS2 | Other_2_Address_Town | Addresses |  |
| BT2 | Other_2_Address_County | Addresses |  |
| BU2 | Other_2_Address_Country | Addresses |  |
| BW2 | Facebook_ID | Social Media Details |  |
| BX2 | X_ID | Social Media Details |  |
| BY2 | LinkedIn ID | Social Media Details |  |
| BZ2 | YouTube_ID | Social Media Details |  |
| CA2 | Online_Communities | Social Media Details |  |
| CB2 | Google_ID | Social Media Details |  |
| CC2 | Number_Of_Followers | Social Media Details |  |
| CD2 | Number_Of_Following | Social Media Details |  |
| CE2 | Number_Of_Tweets_Sent | Social Media Details |  |
| CF2 | Number_Of_Mentions | Social Media Details |  |
| CG2 | Number_Of_Tweets_With_Tags | Social Media Details |  |
| CH2 | Third_Party_Email_Permission | Communication Permissions |  |
| CI2 | Disclosure_Data_To_Third_Party | Communication Permissions |  |
| CJ2 | Third_Party Disclosure_Consent_Date | Communication Permissions |  |
| CK2 | Type_Of_Third_Party | Communication Permissions |  |
| CL2 | Stop_All_Communication | Communication Permissions |  |
| CM2 | Date_Stop_All_Communication | Communication Permissions |  |
| CN2 | Bounced_Invalid_Email_Address | Communication Permissions |  |
| CO2 | Date_Email_Bounced | Communication Permissions |  |
| CP2 | Consent_to_Use_EDI_Data (Yes/No) | Equality and Diversity |  |
| CQ2 | EDI_Submission_Date | Equality and Diversity |  |
| CR2 | Date_of_Birth | Equality and Diversity |  |
| CS2 | Age_Range | Equality and Diversity |  |
| CT2 | Gender | Equality and Diversity |  |
| CU2 | Self_Described_Gender | Equality and Diversity |  |
| CV2 | Gender Identity Same as the Sex assigned at the Birth (Yes, No, Prefer Not to Say) | Equality and Diversity |  |
| CW2 | Sexual_Orientation | Equality and Diversity |  |
| CX2 | Self_Described_Sexual_Orientation | Equality and Diversity |  |
| CY2 | Marital_Status | Equality and Diversity |  |
| CZ2 | Ethnicity_Type | Equality and Diversity |  |
| DA2 | Ethnicity | Equality and Diversity |  |
| DB2 | Other_Ethnicity | Equality and Diversity |  |
| DC2 | Socio-economic_Variables (Yes, No, I don't Know, Prefer Not to Say) | Equality and Diversity |  |
| DD2 | Caring Responsibility | Equality and Diversity |  |
| DE2 | Profile_1 | Profile |  |
| DF2 | Profile_2 | Profile |  |
| DG2 | Profile_3 | Profile |  |
| DH2 | Profile_4 | Profile |  |
| DI2 | Profile_5 | Profile |  |
| DJ2 | Do_Not_Show_In_Directory (Yes/No) | Directory Details |  |
| DK2 | Use_Mailing_Address_As_Directory(Yes/No) | Directory Details |  |
| DL2 | Use_Billing_Address_As_Directory(Yes/No) | Directory Details |  |
| DM2 | Directory_Name | Directory Details |  |
| DN2 | Directory_Address_Postcode | Directory Details |  |
| DO2 | Directory_Address_Line_1 | Directory Details |  |
| DP2 | Directory_Address_Line_2 | Directory Details |  |
| DQ2 | Directory_Address_Line_3 | Directory Details |  |
| DR2 | Directory_Address_Line_4 | Directory Details |  |
| DS2 | Directory_Address_Town | Directory Details |  |
| DT2 | Directory_Address_County | Directory Details |  |
| DU2 | Directory_Address_Country | Directory Details |  |
| DV2 | Directory_Primary_Telephone | Directory Details |  |
| DW2 | Directory_Secondary_Telephone | Directory Details |  |
| DX2 | Directory_Mobile_1 | Directory Details |  |
| DY2 | Directory_Mobile_2 | Directory Details |  |
| DZ2 | Directory_Primary_Email | Directory Details |  |
| EA2 | Directory_Secondary_Email | Directory Details |  |
| EB2 | Directory_Fax | Directory Details |  |
| EC2 | Directory_Website | Directory Details |  |
| ED2 | Directory_Facebook_ID | Directory Details |  |
| EE2 | Directory_Twitter_ID | Directory Details |  |
| EF2 | Directory_LinkedIn_ID | Directory Details |  |
| EG2 | Directory_YouTube_ID | Directory Details |  |
| EH2 | Member_ID | Membership General Information |  |
| EI2 | Application_Date | Membership General Information |  |
| EJ2 | Join_Date | Membership General Information |  |
| EK2 | Member_Type | Membership General Information |  |
| EL2 | Member_Category | Membership General Information |  |
| EM2 | Category_Date | Membership General Information |  |
| EN2 | Subscription_Band | Membership General Information |  |
| EO2 | Subscription_Band_Date | Membership General Information |  |
| EP2 | Member_Status | Membership General Information |  |
| EQ2 | Status_Date | Membership General Information |  |
| ER2 | Archive(Yes/No) | Membership General Information |  |
| ES2 | Resign_Reason | Membership General Information |  |
| ET2 | Resign_Date | Membership General Information |  |
| EU2 | Reinstatement_Date | Membership General Information |  |
| EV2 | Re_Join_Date | Membership General Information |  |
| EW2 | Exempt_Joining_Fee (Yes/No) | Membership General Information |  |
| EX2 | Free_Membership (Yes/No) | Membership General Information |  |
| EY2 | Membership_Pack_Sent (Yes/No) | Membership General Information |  |
| EZ2 | Membership_Pack_Sent_Date | Membership General Information |  |
| FA2 | Exclude_from_Renewal(Yes/No) | Membership General Information |  |
| FB2 | Exclude_from_Lapse (Yes/No) | Membership General Information |  |
| FC2 | Remarks | Membership General Information |  |
| FD2 | Renewal_Month | Membership Finance Information |  |
| FE2 | Renewal_Date | Membership Finance Information |  |
| FF2 | Next_Renewal_Date | Membership Finance Information |  |
| FG2 | Promotion_Code | Membership Finance Information |  |
| FH2 | Payment_Method | Membership Finance Information |  |
| FI2 | Payment_Frequency | Membership Finance Information |  |
| FJ2 | Apply_Rate_to_Paid_By (Yes/No) | Membership Finance Information |  |
| FK2 | Paid_By | Membership Finance Information |  |
| FL2 | Invoice_to_Account (Yes/No) | Membership Finance Information |  |
| FM2 | Invoicing_Account | Membership Finance Information |  |
| FN2 | Paid_Till_Date | Membership Finance Information |  |
| FO2 | Last_Payment_Amount | Membership Finance Information |  |
| FP2 | Last_Payment_Date | Membership Finance Information |  |
| FQ2 | Last_Payment_Method | Membership Finance Information |  |
| FR2 | Total_Outstanding | Membership Finance Information |  |
| FS2 | Bank | Membership Direct Debit Information |  |
| FT2 | Current_Bank_Sort_Code | Membership Direct Debit Information |  |
| FU2 | Current_Account_Number | Membership Direct Debit Information |  |
| FV2 | Current_Account_Title | Membership Direct Debit Information |  |
| FW2 | Mandate_On_Date | Membership Direct Debit Information |  |
| FX2 | First_Collection_Date | Membership Direct Debit Information |  |
| FY2 | Regular_Collection_Date | Membership Direct Debit Information |  |
| FZ2 | Direct_Debit_Suspended (Yes/No) | Membership Direct Debit Information |  |
| GA2 | Instalment_Recalculated_Date | Membership Direct Debit Information |  |
| GB2 | Total_Instalments | Membership Direct Debit Information |  |
| GC2 | Instalments_Paid | Membership Direct Debit Information |  |
| GD2 | BACS_Reference | Membership Direct Debit Information |  |
| GE2 | Direct_Debit_Flag | Membership Direct Debit Information |  |

## Contact Disabilitities

Exact source sheet name: `Contact Disabilitities`. Header row 2. 5 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| B2 | Contact_ID | Contact Disabilitities | Yes |
| C2 | Considered_as_Disabled | Contact Disabilitities |  |
| D2 | Disability | Contact Disabilitities |  |
| E2 | Other_Disability | Contact Disabilitities |  |
| F2 | Description | Contact Disabilitities |  |

## Contact Qualifications

Exact source sheet name: ` Contact Qualifications`. Header row 2. 9 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| B2 | Contact_ID | Contact Qualifications | Yes |
| C2 | Qualification_Type | Contact Qualifications |  |
| D2 | Account ID | Contact Qualifications |  |
| E2 | Other_Institution | Contact Qualifications |  |
| F2 | Awarding_Country | Contact Qualifications |  |
| G2 | Start_Date | Contact Qualifications |  |
| H2 | End Date | Contact Qualifications |  |
| I2 | Date_Of_Qualification_Awarded | Contact Qualifications |  |
| J2 | Description | Contact Qualifications |  |

## Contact Working Patterns

Exact source sheet name: `Contact Working Patterns`. Header row 2. 4 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| B2 | Contact_ID | Contact Working Patterns | Yes |
| C2 | Working_Pattern | Contact Working Patterns |  |
| D2 | Self_Described_Working_Pattern | Contact Working Patterns |  |
| E2 | Description | Contact Working Patterns |  |

## Contact CPD Details

Exact source sheet name: `Contact CPD Details`. Header row 2. 12 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| B2 | Contact_ID | Contact CPD Details | Yes |
| C2 | Date_Awarded | Contact CPD Details |  |
| D2 | Event_ID | Contact CPD Details |  |
| E2 | Event_Programme | Contact CPD Details |  |
| F2 | Event_Delegate | Contact CPD Details |  |
| G2 | CPD_Hours | Contact CPD Details |  |
| H2 | Area_of_Development | Contact CPD Details |  |
| I2 | Development_Objectives | Contact CPD Details |  |
| J2 | What_did_I_do | Contact CPD Details |  |
| K2 | What_did_I_Achieve | Contact CPD Details |  |
| L2 | Any_Further_Action_Required | Contact CPD Details |  |
| M2 | Description | Contact CPD Details |  |

## Communicatioin History

Exact source sheet name: `Communicatioin History`. Header row 2. 17 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| B2 | Contact_ID | Communication History | Yes |
| C2 | Account_ID | Communication History | Yes |
| D2 | Category | Communication History |  |
| E2 | Description | Communication History |  |
| F2 | Date_Of_Communication | Communication History |  |
| G2 | Time Duration | Communication History |  |
| H2 | Owner | Communication History |  |
| I2 | Communication_Details | Communication History |  |
| J2 | Task | Communication History |  |
| K2 | Event_Registration | Communication History |  |
| L2 | Meeting | Communication History |  |
| M2 | Direction | Communication History |  |
| N2 | Nature | Communication History |  |
| O2 | Mode | Communication History |  |
| P2 | User_Name | Communication History |  |
| Q2 | Remarks | Communication History |  |
| R2 | Type (Note, Call, Email, Meeting, Task) | Communication History |  |

## Profile List

Exact source sheet name: `Profile List`. Header row 3. 2 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| B3 | Parent_Node | Fields | Yes |
| C3 | Child_Node | Fields | Yes |

## Account Contact Relationship

Exact source sheet name: `Account Contact Relationship`. Header row 2. 10 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| B2 | Contact_ID | Account Contact Relationship | Yes |
| C2 | Account_ID | Account Contact Relationship | Yes |
| D2 | Department | Account Contact Relationship |  |
| E2 | Job_Title | Account Contact Relationship |  |
| F2 | Description | Account Contact Relationship |  |
| G2 | Main_Contact (Yes/No) | Account Contact Relationship |  |
| H2 | Main_Account (Yes/No) | Account Contact Relationship |  |
| I2 | Effective_Date | Account Contact Relationship |  |
| J2 | Ceased(Yes/No) | Account Contact Relationship |  |
| K2 | Ceased_Date | Account Contact Relationship |  |

## ACCOUNT

Exact source sheet name: `ACCOUNT`. Header row 2. 160 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| A2 | Parent_Account | General Information |  |
| B2 | Parent_Relationship | General Information |  |
| C2 | Child_Relationship | General Information | Yes |
| D2 | Effective_Date | General Information | Yes |
| E2 | Account_ID | General Information | Yes |
| F2 | Account_Name | General Information | Yes |
| G2 | Account_Full_Name | General Information |  |
| H2 | Trading_As | General Information |  |
| I2 | Trading_Status | General Information |  |
| J2 | Account_Type | General Information |  |
| K2 | Industry_Sector | General Information |  |
| L2 | Date_Business_Commenced | General Information |  |
| M2 | Account_Owner | General Information |  |
| N2 | Venue | General Information |  |
| O2 | Main_Contact | General Information |  |
| P2 | Main_Contact_Department | General Information |  |
| Q2 | Main_Contact_Job_Title | General Information |  |
| R2 | Marketing_Source | General Information |  |
| S2 | Turnover_Range | General Information |  |
| T2 | Number_Of_Employees | General Information |  |
| U2 | Employees_Range | General Information |  |
| V2 | Region | General Information |  |
| W2 | Branch | General Information |  |
| X2 | Old_Reference | General Information |  |
| Y2 | Admin/Primary_Email_Address | Emails |  |
| Z2 | Finance_Email_Address | Emails |  |
| AA2 | Other_Email_1 | Emails |  |
| AB2 | Other_Email_2 | Emails |  |
| AC2 | Other_Email_3_ | Emails |  |
| AD2 | Other_Email_4 | Emails |  |
| AE2 | Other_Emial_5 | Emails |  |
| AF2 | Website | Website, Mobile,Telephone and Fax |  |
| AG2 | Mobile | Website, Mobile,Telephone and Fax |  |
| AH2 | Telephone1 | Website, Mobile,Telephone and Fax |  |
| AI2 | Telephone2 | Website, Mobile,Telephone and Fax |  |
| AJ2 | Telephone3 | Website, Mobile,Telephone and Fax |  |
| AK2 | Telephone4 | Website, Mobile,Telephone and Fax |  |
| AL2 | Telephone5 | Website, Mobile,Telephone and Fax |  |
| AM2 | Fax | Website, Mobile,Telephone and Fax |  |
| AN2 | Mailing_Address_Contact_Name | Addresses |  |
| AO2 | Mailing_Address_Job_Title | Addresses |  |
| AP2 | Mailing_Address_Postcode | Addresses |  |
| AQ2 | Mailing_Address_Line_1 | Addresses |  |
| AR2 | Mailing_Address_Line_2 | Addresses |  |
| AS2 | Mailing_Address_Line_3 | Addresses |  |
| AT2 | Mailing_Address_Line_4 | Addresses |  |
| AU2 | Mailing_Address_Town | Addresses |  |
| AV2 | Mailing_Address_County | Addresses |  |
| AW2 | Mailing_Address_Country | Addresses |  |
| AX2 | Mailing_And_Billing_Address_Are_Same_(Yes/No) | Addresses |  |
| AY2 | Billing_Address_Contact_Name | Addresses |  |
| AZ2 | Billing_Address_Job_Title | Addresses |  |
| BA2 | Billing_Address_Postcode | Addresses |  |
| BB2 | Billing_Address_Line_1 | Addresses |  |
| BC2 | Billing_Address_Line_2 | Addresses |  |
| BD2 | Billing_Address_Line_3 | Addresses |  |
| BE2 | Billing_Address_Line_4 | Addresses |  |
| BF2 | Billing_Address_Town | Addresses |  |
| BG2 | Billing_Address_County | Addresses |  |
| BH2 | Billing_Address_Country | Addresses |  |
| BI2 | Facebook_ID | Social Media Details |  |
| BJ2 | X_ID | Social Media Details |  |
| BK2 | LinkedIn_ID | Social Media Details |  |
| BL2 | YouTube_ID | Social Media Details |  |
| BM2 | Online_Communities | Social Media Details |  |
| BN2 | Google_ID | Social Media Details |  |
| BO2 | Number_Of_Followers | Social Media Details |  |
| BP2 | Number_Of_Following | Social Media Details |  |
| BQ2 | Number_Of_Tweets_Sent | Social Media Details |  |
| BR2 | Number_Of_Mentions | Social Media Details |  |
| BS2 | Number_Of_Tweets_With_Tags | Social Media Details |  |
| BT2 | Third_Party_Email_Permission_(Yes/No) | Communication Permissions |  |
| BU2 | Disclosure_Data_To_Third_Party_(Yes/No) | Communication Permissions |  |
| BV2 | Third_Party_Disclosure_Consent_Date | Communication Permissions |  |
| BW2 | Type_Of_Third_Party | Communication Permissions |  |
| BX2 | Stop_All_Communication_(Yes/No) | Communication Permissions |  |
| BY2 | Date_Stop_All_Communication | Communication Permissions |  |
| BZ2 | Bounced/Invalid_Email_Address_(Yes/No) | Communication Permissions |  |
| CA2 | Date_Email_Bounced | Communication Permissions |  |
| CB2 | Do_Not_Show_In_Directory_(Yes/No) | Directory Details |  |
| CC2 | Use_Mailing_Address_As_Directory(Yes/No) | Directory Details |  |
| CD2 | Use_Billing_Address_As_Directory_(Yes/No) | Directory Details |  |
| CE2 | Directory_Name | Directory Details |  |
| CF2 | Directory_Address_Postcode | Directory Details |  |
| CG2 | Directory_Address_Line_1 | Directory Details |  |
| CH2 | Directory_Address_Line_2 | Directory Details |  |
| CI2 | Directory_Address_Line_3 | Directory Details |  |
| CJ2 | Directory_Address_Line_4 | Directory Details |  |
| CK2 | Directory_Address_Town | Directory Details |  |
| CL2 | Directory_Address_County | Directory Details |  |
| CM2 | Directory_Address_Country | Directory Details |  |
| CN2 | Directory_Primary_Telephone | Directory Details |  |
| CO2 | Directory_Secondary_Telephone | Directory Details |  |
| CP2 | Directory_Mobile_1 | Directory Details |  |
| CQ2 | Directory_Mobile_2 | Directory Details |  |
| CR2 | Directory_Primary_Email | Directory Details |  |
| CS2 | Directory_Secondary_Email | Directory Details |  |
| CT2 | Directory_Fax | Directory Details |  |
| CU2 | Directory_Website | Directory Details |  |
| CV2 | Directory_Facebook_ID | Directory Details |  |
| CW2 | Directory_Twitter_ID | Directory Details |  |
| CX2 | Directory_LinkedIn_ID | Directory Details |  |
| CY2 | Directory_YouTube_ID | Directory Details |  |
| CZ2 | Profile_1 | Profile |  |
| DA2 | Profile_2 | Profile |  |
| DB2 | Profile_3 | Profile |  |
| DC2 | Profile_4 | Profile |  |
| DD2 | Profile_5 | Profile |  |
| DE2 | Member_ID | Membership General Information | Yes |
| DF2 | Application_Date | Membership General Information |  |
| DG2 | Join_Date | Membership General Information |  |
| DH2 | Member_Type | Membership General Information |  |
| DI2 | Member_Category | Membership General Information |  |
| DJ2 | Category_Date | Membership General Information |  |
| DK2 | Subscription_Band | Membership General Information |  |
| DL2 | Subscription_Band_Date | Membership General Information |  |
| DM2 | Member_Status | Membership General Information |  |
| DN2 | Status_Date | Membership General Information |  |
| DO2 | Primary_Membership_(Yes/No) | Membership General Information |  |
| DP2 | Archive(Yes/No) | Membership General Information |  |
| DQ2 | Resign_Reason | Membership General Information |  |
| DR2 | Resign_Date | Membership General Information |  |
| DS2 | Reinstatement_Date | Membership General Information |  |
| DT2 | Re-Join_Date | Membership General Information |  |
| DU2 | Exempt_Joining_Fee_(Yes/No) | Membership General Information |  |
| DV2 | Free_Membership_(Yes/No) | Membership General Information |  |
| DW2 | Membership_Pack_Sent_(Yes/No) | Membership General Information |  |
| DX2 | Membership_Pack_Sent_Date | Membership General Information |  |
| DY2 | Exclude_from_Renewal(Yes/No) | Membership General Information |  |
| DZ2 | Exclude_from_Lapse_(Yes/No) | Membership General Information |  |
| EA2 | Remarks | Membership General Information |  |
| EB2 | Renewal_Month | Membership Finance Information |  |
| EC2 | Renewal_Date | Membership Finance Information |  |
| ED2 | Next_Renewal_Date | Membership Finance Information |  |
| EE2 | Promotion_Code | Membership Finance Information |  |
| EF2 | Payment_Method | Membership Finance Information |  |
| EG2 | Payment_Frequency | Membership Finance Information |  |
| EH2 | Apply_Rate_to_Paid_By_(Yes/No) | Membership Finance Information |  |
| EI2 | Paid_By | Membership Finance Information |  |
| EJ2 | Invoice_to_Account_(Yes/No) | Membership Finance Information |  |
| EK2 | Invoicing_Account | Membership Finance Information |  |
| EL2 | Paid_Till_Date | Membership Finance Information |  |
| EM2 | Last_Payment_Amount | Membership Finance Information |  |
| EN2 | Last_Payment_Date | Membership Finance Information |  |
| EO2 | Last_Payment_Method | Membership Finance Information |  |
| EP2 | Total_Outstanding | Membership Finance Information |  |
| EQ2 | Bank | Membership Direct Debit Information |  |
| ER2 | Current_Bank_Sort_Code | Membership Direct Debit Information |  |
| ES2 | Current_Account_Number | Membership Direct Debit Information |  |
| ET2 | Current_Account_Title | Membership Direct Debit Information |  |
| EU2 | Mandate_On_Date | Membership Direct Debit Information |  |
| EV2 | First_Collection_Date | Membership Direct Debit Information |  |
| EW2 | Regular_Collection_Date | Membership Direct Debit Information |  |
| EX2 | Direct_Debit_Suspended_(Yes/No) | Membership Direct Debit Information |  |
| EY2 | Instalment_Recalculated_Date | Membership Direct Debit Information |  |
| EZ2 | Total_Instalments | Membership Direct Debit Information |  |
| FA2 | Instalments_Paid | Membership Direct Debit Information |  |
| FB2 | BACS_Reference | Membership Direct Debit Information |  |
| FC2 | Direct_Debit_Flag | Membership Direct Debit Information |  |
| FD2 | Include_In_Mandate_File_(Yes/No) | Membership Direct Debit Information |  |

## Membership

Exact source sheet name: `Membership`. Header row 2. 54 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| B2 | Member_ID | General Informatioin | Yes |
| C2 | Contact_ID | General Informatioin | Yes |
| D2 | Account_ID | General Informatioin | Yes |
| E2 | Application_Date | General Informatioin |  |
| F2 | Join_Date | General Informatioin |  |
| G2 | Member_Type | General Informatioin |  |
| H2 | Member_Category | General Informatioin |  |
| I2 | Category_Date | General Informatioin |  |
| J2 | Subscription_Band | General Informatioin |  |
| K2 | Subscription_Band_Date | General Informatioin |  |
| L2 | Member_Status | General Informatioin |  |
| M2 | Status_Date | General Informatioin |  |
| N2 | Primary_Membership_(Yes/No) | General Informatioin |  |
| O2 | Archive(Yes/No) | General Informatioin |  |
| P2 | Resign_Reason | General Informatioin |  |
| Q2 | Resign_Date | General Informatioin |  |
| R2 | Reinstatement_Date | General Informatioin |  |
| S2 | Re-Join_Date | General Informatioin |  |
| T2 | Exempt_Joining_Fee_(Yes/No) | General Informatioin |  |
| U2 | Free_Membership_(Yes/No) | General Informatioin |  |
| V2 | Membership_Pack_Sent_(Yes/No) | General Informatioin |  |
| W2 | Membership_Pack_Sent_Date | General Informatioin |  |
| X2 | Exclude_from_Renewal(Yes/No) | General Informatioin |  |
| Y2 | Exclude_from_Lapse_(Yes/No) | General Informatioin |  |
| Z2 | Remarks | General Informatioin |  |
| AA2 | Renewal_Month | Finance |  |
| AB2 | Renewal_Date | Finance |  |
| AC2 | Next_Renewal_Date | Finance |  |
| AD2 | Promotion_Code | Finance |  |
| AE2 | Payment_Method | Finance |  |
| AF2 | Payment_Frequency | Finance |  |
| AG2 | Apply_Rate_to_Paid_By_(Yes/No) | Finance |  |
| AH2 | Paid_By | Finance |  |
| AI2 | Invoice_to_Account_(Yes/No) | Finance |  |
| AJ2 | Invoicing_Account | Finance |  |
| AK2 | Paid_Till_Date | Finance |  |
| AL2 | Last_Payment_Amount | Finance |  |
| AM2 | Last_Payment_Date | Finance |  |
| AN2 | Last_Payment_Method | Finance |  |
| AO2 | Total_Outstanding | Finance |  |
| AP2 | Bank | Direct Debit |  |
| AQ2 | Last_Bank_Sort_Code | Direct Debit |  |
| AR2 | Last_Account_Number | Direct Debit |  |
| AS2 | Last_Account_Title | Direct Debit |  |
| AT2 | Mandate_On_Date | Direct Debit |  |
| AU2 | First_Collection_Date | Direct Debit |  |
| AV2 | Regular_Collection_Date | Direct Debit |  |
| AW2 | Direct_Debit_Suspended_(Yes/No) | Direct Debit |  |
| AX2 | Instalment_Recalculated_Date | Direct Debit |  |
| AY2 | Total_Instalments | Direct Debit |  |
| AZ2 | Instalments_Paid | Direct Debit |  |
| BA2 | BACS_Reference | Direct Debit |  |
| BB2 | Direct_Debit_Flag | Direct Debit |  |
| BC2 | Include_In_Mandate_File_(Yes/No) | Direct Debit |  |

## DD Instalments

Exact source sheet name: `DD Instalments`. Header row 2. 5 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| B2 | Member_ID | Fields | Yes |
| C2 | Instalment No | Fields |  |
| D2 | Amount | Fields |  |
| E2 | Paid | Fields |  |
| F2 | Due Date | Fields |  |

## Membership Application

Exact source sheet name: `Membership Application`. Header row 2. 23 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| B2 | Member_ID | Membership Application | Yes |
| C2 | Member_Name | Membership Application |  |
| D2 | Contact_ID | Membership Application | Yes |
| E2 | Account_ID | Membership Application | Yes |
| F2 | Member_Type | Membership Application |  |
| G2 | Member_Category | Membership Application |  |
| H2 | Subscription_Band | Membership Application |  |
| I2 | Payment_Method | Membership Application |  |
| J2 | Payment_Frequency | Membership Application |  |
| K2 | Invoice_to_Account_(Yes/No) | Membership Application |  |
| L2 | Invoicing_Account | Membership Application |  |
| M2 | Amount | Membership Application |  |
| N2 | Application_Date | Membership Application |  |
| O2 | Application_Type(New,_Upgrade,_Reinstate,_Rejoin) | Membership Application |  |
| P2 | Application_Status | Membership Application |  |
| Q2 | Status_Date | Membership Application |  |
| R2 | Exempt_Joining_Fee(Yes/No) | Membership Application |  |
| S2 | Remarks | Membership Application |  |
| T2 | Description | Membership Application |  |
| U2 | Bank | Membership Application |  |
| V2 | Account_Title | Membership Application |  |
| W2 | Sort_Code | Membership Application |  |
| X2 | Account_Number | Membership Application |  |

## Membership Rate

Exact source sheet name: `Membership Rate`. Header row 2. 13 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| B2 | Description | Membership Rate |  |
| C2 | Member_Rate_Type | Membership Rate |  |
| D2 | Fiscal_Period | Membership Rate |  |
| E2 | Member_Type | Membership Rate |  |
| F2 | Member_Category | Membership Rate |  |
| G2 | Member_Subscription_Band | Membership Rate |  |
| H2 | VAT_Applicable_(Yes/No) | Membership Rate |  |
| I2 | VAT_Inclusive_(yes/No) | Membership Rate |  |
| J2 | VAT_Rate | Membership Rate |  |
| K2 | Nominal_Code | Membership Rate |  |
| L2 | Cost_Centre | Membership Rate |  |
| M2 | Rate_Amount | Membership Rate |  |
| N2 | Renewal_Rate_Amount | Membership Rate |  |

## Membership History

Exact source sheet name: `Membership History`. Header row 2. 11 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| B2 | Member_ID | Membership History | Yes |
| C2 | Member_Type | Membership History |  |
| D2 | Category | Membership History |  |
| E2 | Subscription_Band | Membership History |  |
| F2 | Status | Membership History |  |
| G2 | Resign_Reason | Membership History |  |
| H2 | Type(Member_Type,Category,Subscription_Band,Status) | Membership History | Yes |
| I2 | Effective_Date | Membership History |  |
| J2 | Ceased_Date | Membership History |  |
| K2 | Days | Membership History | Yes |
| L2 | Description | Membership History |  |

## Events

Exact source sheet name: `Events`. Header row 2. 54 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| B2 | Event_Type | Event |  |
| C2 | Event_ID | Event | Yes |
| D2 | Event_Name | Event | Yes |
| E2 | Formal_Title | Event |  |
| F2 | Event_Status | Event |  |
| G2 | Main_Contact_(ID) | Event |  |
| H2 | Coordinator_Phone_Number | Event |  |
| I2 | Event_Format(In_Person,_Online,_Hybrid) | Event |  |
| J2 | Separate_Seats_for_In_Person_and_Online_(Yes/No) | Event |  |
| K2 | Allow_Abstracts_(Yes/No) | Event |  |
| L2 | Event_Location | Event |  |
| M2 | Venue (Account_ID) | Event |  |
| N2 | Location | Event |  |
| O2 | Time_Zone | Event |  |
| P2 | CPD_By | Event |  |
| Q2 | Event_CPD_Hours | Event |  |
| R2 | CPD_Assigned_to_Delegates | Event |  |
| S2 | Remarks | Event |  |
| T2 | Start_Date | Key Dates | Yes |
| U2 | End_Date | Key Dates | Yes |
| V2 | Start_Time | Key Dates |  |
| W2 | End_Time | Key Dates |  |
| X2 | Show_on_Web_(Yes/No) | Key Dates |  |
| Y2 | Show_on_Web_Date | Key Dates |  |
| Z2 | Closing_Date | Key Dates |  |
| AA2 | Closing_Date_for_Web | Key Dates |  |
| AB2 | Last_Date_to_Transfer | Key Dates |  |
| AC2 | Last_Date_to_Withdraw | Key Dates |  |
| AD2 | Last_Date_to_Cancel | Key Dates |  |
| AE2 | Last_Date_to_Submit_Abstract | Key Dates |  |
| AF2 | Last_Date_to_Update_Delegates | Key Dates |  |
| AG2 | Minimum_Seats_-_In_Person | Seats In Person, Onlile, Hybrid |  |
| AH2 | Maximum_Seats_-_In_Person | Seats In Person, Onlile, Hybrid |  |
| AI2 | Waiting_Limit_Number_-_In_Person | Seats In Person, Onlile, Hybrid |  |
| AJ2 | Maximum_Additional_Delegates_-_In_Person | Seats In Person, Onlile, Hybrid |  |
| AK2 | Minimum_Seats_Online | Seats In Person, Onlile, Hybrid |  |
| AL2 | Maximum_Seats_Online | Seats In Person, Onlile, Hybrid |  |
| AM2 | Waiting_Limit_Number_-_Online | Seats In Person, Onlile, Hybrid |  |
| AN2 | Maximum_Additional_Delegates_-_Online | Seats In Person, Onlile, Hybrid |  |
| AO2 | Minimum_Seats_-_Hybrid | Seats In Person, Onlile, Hybrid |  |
| AP2 | Maximum_Seats_-_Hybrid | Seats In Person, Onlile, Hybrid |  |
| AQ2 | Waiting_Limit_Number_-_Hybrid | Seats In Person, Onlile, Hybrid |  |
| AR2 | Maximum_Additional_Delegates_-_Hybrid | Seats In Person, Onlile, Hybrid |  |
| AS2 | Event_Overview | Event Information on Web |  |
| AT2 | View_Counter | Event Information on Web |  |
| AU2 | External_Event_(Yes/No) | Event Information on Web |  |
| AV2 | External_Booking_URL | Event Information on Web |  |
| AW2 | Terms_/_Conditions_for_Web | Event Information on Web |  |
| AX2 | Notes_for_Web | Event Information on Web |  |
| AY2 | Cost_Centre | Finance |  |
| AZ2 | Ledger_Account | Finance |  |
| BA2 | VAT_Rate | Finance |  |
| BB2 | Planned_Expenses | Finance |  |
| BC2 | Actual_Expenses | Finance |  |

## Event Delegates

Exact source sheet name: `Event Delegates`. Header row 2. 53 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| B2 | Event_ID | Event Delegates | Yes |
| C2 | Booker_(Yes/No) | Event Delegates |  |
| D2 | Booker(contact_ID) | Event Delegates |  |
| E2 | Delegate_(Contact_ID) | Event Delegates | Yes |
| F2 | Booker_Type | Event Delegates |  |
| G2 | Delegate_Type | Event Delegates |  |
| H2 | TBA | Event Delegates |  |
| I2 | Registration_Date | Event Delegates |  |
| J2 | Registration_Status | Event Delegates |  |
| K2 | Status_Date | Event Delegates |  |
| L2 | Booking_Format_(In_Person,_Online) | Event Delegates |  |
| M2 | Account | Event Delegates |  |
| N2 | Attended | Event Delegates |  |
| O2 | Remarks | Event Delegates |  |
| P2 | Include_on_participation_list | Event Delegates |  |
| Q2 | Receive_mailings_from_sponsor | Event Delegates |  |
| R2 | Special_Requirements | Event Delegates |  |
| S2 | Description | Event Delegates |  |
| T2 | Session_1 | Sessions Attended |  |
| U2 | Session_2 | Sessions Attended |  |
| V2 | Session_3 | Sessions Attended |  |
| W2 | Session_4 | Sessions Attended |  |
| X2 | Session_5 | Sessions Attended |  |
| Y2 | Session_6 | Sessions Attended |  |
| Z2 | Session_7 | Sessions Attended |  |
| AA2 | Session_8 | Sessions Attended |  |
| AB2 | Session_9 | Sessions Attended |  |
| AC2 | Session_10 | Sessions Attended |  |
| AD2 | Rate_Type_1 | Rate Applied |  |
| AE2 | Rate_Amount_1 | Rate Applied |  |
| AF2 | Vat_Applicable_(Yes/No)_1 | Rate Applied |  |
| AG2 | VAT_RATE_1 | Rate Applied |  |
| AH2 | VAT_Inclusive_(Yes/No)_1 | Rate Applied |  |
| AI2 | Rate_Type_2 | Rate Applied |  |
| AJ2 | Rate_Amount_2 | Rate Applied |  |
| AK2 | VAT_RATE_2 | Rate Applied |  |
| AL2 | Vat_Applicable_(Yes/No)_2 | Rate Applied |  |
| AM2 | VAT_Inclusive_(Yes/No)__2 | Rate Applied |  |
| AN2 | Rate_Type_3 | Rate Applied |  |
| AO2 | Rate_Amount_3 | Rate Applied |  |
| AP2 | VAT_RATE_3 | Rate Applied |  |
| AQ2 | Vat_Applicable_(Yes/No)_3 | Rate Applied |  |
| AR2 | VAT_Inclusive_(Yes/No)__3 | Rate Applied |  |
| AS2 | Rate_Type_4 | Rate Applied |  |
| AT2 | Rate_Amount_4 | Rate Applied |  |
| AU2 | VAT_RATE_4 | Rate Applied |  |
| AV2 | Vat_Applicable_(Yes/No)_4 | Rate Applied |  |
| AW2 | VAT_Inclusive_(Yes/No)__4 | Rate Applied |  |
| AX2 | Rate_Type_5 | Rate Applied |  |
| AY2 | Rate_Amount_5 | Rate Applied |  |
| AZ2 | VAT_RATE_5 | Rate Applied |  |
| BA2 | Vat_Applicable_(Yes/No)_5 | Rate Applied |  |
| BB2 | VAT_Inclusive_(Yes/No)__5 | Rate Applied |  |

## Committee

Exact source sheet name: `Committee`. Header row 2. 11 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| B2 | Committee_ID | Committee | Yes |
| C2 | Committee_Name | Committee | Yes |
| D2 | Committee_Type | Committee |  |
| E2 | Committee_Status | Committee |  |
| F2 | Total_Active_Members | Committee |  |
| G2 | Start_Date | Committee |  |
| H2 | Termination_Date | Committee |  |
| I2 | Purpose | Committee |  |
| J2 | Account_Manager_(CONTACT_ID) | Committee |  |
| K2 | Organiser_(ACCOUNT_ID) | Committee |  |
| L2 | Branch | Committee |  |

## Committee Member

Exact source sheet name: `Committee Member`. Header row 2. 16 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| B2 | Committee_(ID) | Committee Member | Yes |
| C2 | Committee_Member_(CONTACT_ID) | Committee Member | Yes |
| D2 | Member_Type | Committee Member |  |
| E2 | Member_Designation | Committee Member |  |
| F2 | Designation_Date | Committee Member |  |
| G2 | Remarks | Committee Member |  |
| H2 | Member_From | Committee Member |  |
| I2 | End_of_Term | Committee Member |  |
| J2 | Re-election_Date | Committee Member |  |
| K2 | Reason_for_Leaving | Committee Member |  |
| L2 | Active_Member | Committee Member |  |
| M2 | Description | Committee Member |  |
| N2 | Source | Committee Member |  |
| O2 | Co-opted | Committee Member |  |
| P2 | Automated_Committee | Committee Member |  |
| Q2 | Automated_Designation | Committee Member |  |

## Committee Meeting

Exact source sheet name: `Committee Meeting`. Header row 2. 13 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| B2 | Committee_ID | Committee Meeting | Yes |
| C2 | Meeting_Description | Committee Meeting |  |
| D2 | Meeting_Coordinator_(CONTACT_ID) | Committee Meeting |  |
| E2 | Purpose | Committee Meeting |  |
| F2 | Meeting_Code | Committee Meeting |  |
| G2 | Venue_(ACCOUNT_ID) | Committee Meeting |  |
| H2 | Date | Committee Meeting |  |
| I2 | Start_Time | Committee Meeting |  |
| J2 | End_Time | Committee Meeting |  |
| K2 | Contact_ID | Committee Meeting Attendee | Yes |
| L2 | Attendance_Status | Committee Meeting Attendee |  |
| M2 | Remarks | Committee Meeting Attendee |  |
| N2 | Description | Committee Meeting Attendee |  |

## Committee Election

Exact source sheet name: `Committee Election`. Header row 2. 16 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| B2 | Committee_ID | Committee Election | Yes |
| C2 | Election_Post | Committee Election | Yes |
| D2 | Election_Date | Committee Election |  |
| E2 | Description | Committee Election |  |
| F2 | From | Committee Election |  |
| G2 | To | Committee Election |  |
| H2 | Electorate | Committee Election |  |
| I2 | Elected_Member_(CONTACT_ID) | Committee Election | Yes |
| J2 | Candidate_CONTACT_ID | Election Candidates | Yes |
| K2 | Elected | Election Candidates |  |
| L2 | Candidate_Description | Election Candidates |  |
| M2 | Election_Post | Election Candidates |  |
| N2 | Election_Date | Election Candidates |  |
| O2 | From | Election Candidates |  |
| P2 | To | Election Candidates |  |
| Q2 | Electorate | Election Candidates |  |

## Finance

Exact source sheet name: `Finance`. Header row 1. 25 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| A1 | Transaction_Number | Fields | Yes |
| B1 | Ref_Transaction_Number | Fields |  |
| C1 | Description | Fields |  |
| D1 | Transaction_Type | Fields | Yes |
| E1 | Contact_ID | Fields | Yes |
| F1 | Account_ID | Fields | Yes |
| G1 | Transaction_Amount | Fields |  |
| H1 | Transaction_Date | Fields |  |
| I1 | Transaction_Due_Date | Fields |  |
| J1 | Discount_Amount | Fields |  |
| K1 | Tax | Fields |  |
| L1 | Fiscal_Period | Fields |  |
| M1 | Module | Fields |  |
| N1 | Transaction_Line_Id | Fields |  |
| O1 | Quantity | Fields |  |
| P1 | Price_Per_Unit | Fields |  |
| Q1 | Transaction_Line_Amount | Fields |  |
| R1 | Transaction_Line_Discount | Fields |  |
| S1 | Transaction_Line_TAX | Fields |  |
| T1 | Nominal_code | Fields |  |
| U1 | Cost_Centre | Fields |  |
| V1 | Payment_Method_Name | Fields |  |
| W1 | Transaction_Reference | Fields |  |
| X1 | Status(Current,Updated) | Fields |  |
| Y1 | VAT | Fields |  |

## Product Sale

Exact source sheet name: `Product Sale`. Header row 1. 19 field occurrences.

| Cell | Source field | Section | Red heading |
| --- | --- | --- | --- |
| A1 | Product_ID | Fields | Yes |
| B1 | Product_Name | Fields | Yes |
| C1 | Product_Type | Fields |  |
| D1 | Product_Category | Fields |  |
| E1 | Active_Yes_No | Fields |  |
| F1 | Product_Image_URL | Fields |  |
| G1 | Short_Description | Fields |  |
| H1 | Publish_Start_Date | Fields |  |
| I1 | Publish_End_Date | Fields |  |
| J1 | Nominal_Code | Fields |  |
| K1 | Cost_Centre | Fields |  |
| L1 | Show_On_Web_Yes_No | Fields |  |
| M1 | Member_only_Yes_No | Fields |  |
| N1 | Featured_Product_Yes_No | Fields |  |
| O1 | Service_Yes_No | Fields |  |
| P1 | Price | Fields |  |
| Q1 | Web_Description | Fields |  |
| R1 | Supplier_Account_ID | Fields |  |
| S1 | Third_Party_supplier_Account_ID | Fields |  |
