# Overview

This repository contains a technical exercise that is to be used for reference during a technical interview.
Be prepared to discuss the exercise, the expected results and how you came by them.


## Astronaut Career Tracking System (ACTS)

ACTS is used as a tool to maintain a record of all the People that have served as Astronauts. When serving as an Astronaut, your *Job* (Duty) is tracked by your Rank, Title and the Start and End Dates of the Duty.

The People that exist in this system are not all Astronauts. ACTS maintains a master list of People and Duties that are updated from an external service (not controlled by ACTS). The update schedule is determined by the external service.

## Definitions

1. A person's astronaut assignment is the Astronaut Duty.
	Multiple timeline entries 1-to-many
2. A person's current astronaut information is stored in the Astronaut Detail table.
	1-to-1 (observation: normally 1-to-1 relationships can be merged into one table. Consider it)
3. A person's list of astronaut assignments is stored in the Astronaut Duty table.
	Timeline

## Requirements

##### Enhance the Stargate API (Required)

The REST API is expected to do the following:

1. [x] Retrieve a person by name.   [controller]
2. [x] Retrieve all people.         [controller]
3. [x] Person Add/update a person.  [controller]
	[x] Add by name
	[x] Update by name
	[x] Need to use the pre-processor to determine when to update and prevent duplicates
4. [x] Retrieve Astronaut Duty by name. [controller]
5. [ ] Astronaut Duty               [controller]
	[x] Add by name
	[ ] Edit: currentRank, currentDutyTitle, careerStartDate, careerEndDate


##### Implement a user interface: (Encouraged)

The UI is expected to do the following:

1. [ ] Successfully implement a web application that demonstrates production level quality. Angular is preferred.
2. [ ] Implement call(s) to retrieve an individual's astronaut duties.
3. [ ] Display the progress of the process and the results in a visually sophisticated and appealing manner.

## Tasks

Overview
[x] Examine the code, find and resolve any flaws, if any exist. 
[x] Identify design patterns and follow or change them. 
[x] Provide fix(es) and be prepared to describe the changes.

1. [x] Generate the database
   * This is your source and storage location
2. [x] Enforce the rules
3. [x] Improve defensive coding
4. [ ] Add unit tests
   * identify the most impactful methods requiring tests
   * reach >50% code coverage
5. [x] Implement process logging
   * Log exceptions
   * Log successes
   * Store the logs in the database

## Rules (automated tests)

1. A Person is uniquely identified by their Name.
	*Caveat: Mike Johnson, Steve Smith, David Brown
2. A Person who has not had an astronaut assignment will not have Astronaut records.
3. A Person will only ever hold one current Astronaut Duty Title, Start Date, and Rank at a time.
	*Caveat: Overlapping timelines
4. A Person's Current Duty will not have a Duty End Date.
5. A Person's Previous Duty End Date is set to the [day before] the New Astronaut Duty Start Date 
	when a new Astronaut Duty is received for a Person.
6. A Person is classified as 'Retired' when a Duty Title is 'RETIRED'.
7. A Person's Career End Date is [one day before] the Retired Duty Start Date.
