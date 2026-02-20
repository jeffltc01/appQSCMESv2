# MES version 1 Barcode Language

## Purpose of this document

This document defines the barcode scanning language that was created as part of the MES version 1, which this app is replacing.  This is here mostly as a reference of how things are currently being handled.

In general the format of the commands are FUNCTION a semi-colon and then PARAMETERS FOR THE FUNCTION.  By doing this, we can then tell when the user is scanning a barcode from a vendor, etc because we don't have control over those barcodes.

- **Rolls**
  - INP;2 = Advance the material queue
  - INP;3 = Yes
  - INP;4 = No
  - FLT; Button Stuck, FLT; Gnd Fault, Check Wiring (exact text varies, but FLT; is common)
  - SC:00000001 = A pre-printed barcode is being used instead of printing a label
- **Long Seam**
  - SC;00000001 = A pre-printed barcode that represent the shell that just arrived at the work center
- **Long Seam Inspection**
  - SC;00000001 = A pre-printed barcode that represent the shell that just arrived at the work center
  - D;XXX = Logging a defect where the Defect ID is XXX
  - L;XXX = Logging a defect where the Location ID of the defect is XXX
  - S;1 = Save, technically don't need the 1, but adding it for consistency
  - CL;1 = Clear all defects
  - FD;XXX-YYY-ZZZ = Logging full defect where XXX is DefectCode, YYY is the Characteristic Lookup Id and ZZZ is the location Lookup Id
- **Plasma**
  - Future
- **Fit-Up**
  - SC;00000001 = Add the shell
  - INP;1 = Swap Heads
  - INP;2 = Reset
  - INP;3 = Save
  - We added a new concept to have cards (reusable barcode labels) to identify the heads material, to support that concept:
    - KC;value of the barcode that's setup in the MES App for the card. For example if the barcode on the card is KC;03, then that will be used to lookup which queue row is assoicated with KC;03 and apply that Heat/Coil/Lot to the assembly.
  - We have the need to be able to change the current tank size via scanning, this bullet point is for that concept:
    - TS;120 = Change tank size to 120
    - TS;250
    - TS;320
    - TS;500
    - TS;1000
- **Round Seam Inspection**
  - SC;00000001 = A pre-printed barcode that represent the shell that just arrived at the work center
  - D;XXX = Logging a defect where the Defect ID is XXX
  - L;XXX;C;YYY = Logging a defect where the Location ID of the defect is XXX and the Characteristic ID of the defect is YYY
  - S;1 = Save, technically don't need the 1, but adding it for consistency
  - CL;1 = Clear all defects
  - FD;XXX-YYY-ZZZ = Logging full defect where XXX is DefectCode, YYY is the Characteristic Lookup Id and ZZZ is the location Lookup Id
- **Hydro**
  - NOSHELL;0 = No shell