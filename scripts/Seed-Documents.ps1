$ErrorActionPreference = "Stop"

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$seedDir = Join-Path $projectRoot "artifacts\seed-documents"

if (-not (Test-Path $seedDir)) {
    Write-Host "Creating seed directory at $seedDir"
    New-Item -ItemType Directory -Path $seedDir | Out-Null
}

$leavePolicyContent = @"
# Leave Policy (2026 Edition)

## 1. Annual Leave
All full-time employees are entitled to 20 days of paid annual leave per year.
Leave must be requested at least 2 weeks in advance through the HR portal.

## 2. Sick Leave
Employees receive 10 days of paid sick leave annually.
A medical certificate is required for sick leave exceeding 3 consecutive days.

## 3. Maternity and Paternity Leave
- Maternity Leave: 6 months of paid leave.
- Paternity Leave: 4 weeks of paid leave.
"@

$wfhPolicyContent = @"
# Remote Work and WFH Policy

## 1. Eligibility
All employees who have completed their probationary period are eligible to apply for remote work up to 3 days a week.

## 2. Core Hours
Employees must be online and available during core business hours (10:00 AM to 3:00 PM local time).

## 3. Equipment
The company provides a laptop, a secondary monitor, and a headset. Employees are responsible for maintaining a secure and stable internet connection.
"@

$codeOfConductContent = @"
# Code of Conduct

## 1. Professionalism
We expect all employees to treat colleagues, clients, and partners with respect. Harassment of any kind is strictly prohibited.

## 2. Confidentiality
Employees must protect the confidential information of the company and our clients. Do not share internal documents on public platforms.

## 3. Conflict of Interest
Employees must disclose any potential conflicts of interest to their manager immediately.
"@

$leavePolicyPath = Join-Path $seedDir "leave-policy.md"
$wfhPolicyPath = Join-Path $seedDir "wfh-policy.md"
$codeOfConductPath = Join-Path $seedDir "code-of-conduct.md"

Set-Content -Path $leavePolicyPath -Value $leavePolicyContent -Encoding UTF8
Set-Content -Path $wfhPolicyPath -Value $wfhPolicyContent -Encoding UTF8
Set-Content -Path $codeOfConductPath -Value $codeOfConductContent -Encoding UTF8

Write-Host "Successfully generated 3 seed documents in $seedDir"
Write-Host "- $leavePolicyPath"
Write-Host "- $wfhPolicyPath"
Write-Host "- $codeOfConductPath"
