# Pull Request Checklist

## Purpose

Describe why this change is needed and what outcome it delivers.

## Scope

- [ ] Backend
- [ ] Frontend
- [ ] Database schema/migration
- [ ] CI/CD or deployment scripts
- [ ] Documentation

## Impact Declaration

### Primary domain

- [ ] Operator Data Entry
- [ ] Quality/Ops Setup and Analysis
- [ ] Scheduling
- [ ] Shared cross-domain

### Operator entry impact

- [ ] No operator-entry impact expected
- [ ] Possible indirect impact to operator entry
- [ ] Direct operator-entry change

If any non-"No operator-entry impact expected" option is selected, explain:

- Affected operator workflow(s):
- Risk level (Low/Medium/High):
- Why this change is safe:

## Validation Performed

### Automated tests

- [ ] Backend tests passed (`dotnet test backend/MESv2.Api.Tests`)
- [ ] Frontend tests passed (`npm run test:coverage` in `frontend`)
- [ ] Operator-related tests added/updated when behavior changed

### Operator critical path verification

- [ ] Operator entry route/screen load verified
- [ ] At least one operator submit/save path verified
- [ ] Barcode/manual behavior unaffected (or intentionally changed and documented)
- [ ] API contract changes are backward compatible or explicitly versioned

### Performance and reliability checks

- [ ] Change does not materially regress operator-critical response-time expectations
- [ ] Save/submit path remains idempotent under retry behavior

## Deployment and Rollback Notes

- Feature flag used? (Yes/No):
- Post-deploy smoke steps:
- Rollback or kill-switch plan:

## Linked Work

- Issue(s):
- Related spec/design docs:
