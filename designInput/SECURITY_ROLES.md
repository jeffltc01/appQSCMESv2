# Security Role Definitions for QSC MES

## Purpose of this document

This document defines the security roles for the application at a high level.  The list is organized with the roles that have the most abilities to do things at the top of the list and then get more restricitive as we go down the list.

- **1.0**
  - **Administrator**: This would mainly be Information Technology people that basically have permission to do everything in the App.
- **2.0**
  - **Quality Director**: The director of Quality throughout all 3 plants.  Similar level to the Operations Director within the company, but more focused on quality.
  - **Operations Director**: The director of Operations throughout all 3 plants.  Similar level to the Quality Director within the company, but more focused on building the finished goods.
- **3.0**
  - **Quality Manager**: The Quality Manager's report to the Quality director.  There's only one Quality Manager at each plant.
  - **Plant Manager**: THe Plant Manager's report to the Operations director.  There's only one Plant Manager at each plant.
- **4.0**
  - **Supervisor**: Reports to a single plant's Plant Manager.  There are generally just one or two supervisors at each Plant.
- **5.0**
  - **Quality Tech**: Reports to a single plant's Quality Manager.
  - **Team Lead**: Reports to a Supervisor at a Plant.
- **5.5**
  - **Authorized Inspector**: The Authroized Inspector (AI) is a special case.  This person doesn't not actually work for Quality Steel but is a representative of an outside company that is required to be onsite and ensure the plants are conforming to the ASME standards for making Propane Tanks.  We want this person to have some access to view logs and things, but we need to be careful that they cannot see too much.
- **6.0**
  - **Operator**: Everyone else that does the actually manufacturing work on the plant floor.  This would include work center operators, material handling, etc.  