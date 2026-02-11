# solver-demo
An example of a simple solver for planning schedules

Case 1: Minimal happy-path (2 workers, 1 shift)

{
  "workers": [
    {
      "workerId": "w1",
      "name": "Alice",
      "skills": []
    },
    {
      "workerId": "w2",
      "name": "Bob",
      "skills": []
    }
  ],
  "shifts": [
    {
      "date": "2026-02-11",
      "shiftType": 0,
      "requiredWorkers": 1,
      "preassignedWorkerIds": [],
      "requiredSkills": []
    }
  ],
  "allowMovePreassigned": false,
  "availabilityByWorkerId": {
    "w1": ["2026-02-11:Morning"],
    "w2": ["2026-02-11:Morning"]
  }
}


Call 2: Valid preassignment (hard constraint)

{
  "workers": [
    {
      "workerId": "w1",
      "name": "Alice",
      "skills": []
    },
    {
      "workerId": "w2",
      "name": "Bob",
      "skills": []
    }
  ],
  "shifts": [
    {
      "date": "2026-02-12",
      "shiftType": 0,
      "requiredWorkers": 1,
      "preassignedWorkerIds": ["w1"],
      "requiredSkills": []
    }
  ],
  "allowMovePreassigned": false,
  "availabilityByWorkerId": {
    "w1": ["2026-02-12:Morning"],
    "w2": ["2026-02-12:Morning"]
  }
}


Case 3: Unsolvable due to availability (fast failure)

{
  "workers": [
    {
      "workerId": "w1",
      "name": "Alice",
      "skills": []
    },
    {
      "workerId": "w2",
      "name": "Bob",
      "skills": []
    }
  ],
  "shifts": [
    {
      "date": "2026-02-13",
      "shiftType": 0,
      "requiredWorkers": 1,
      "preassignedWorkerIds": ["w1"],
      "requiredSkills": []
    }
  ],
  "allowMovePreassigned": false,
  "availabilityByWorkerId": {
    "w2": ["2026-02-13:Morning"]
  }
}

Case 4: Larger planning example (7 days, backtracking)
{
  "workers": [
    { "workerId": "w1", "name": "Worker 1", "skills": [] },
    { "workerId": "w2", "name": "Worker 2", "skills": [] },
    { "workerId": "w3", "name": "Worker 3", "skills": [] },
    { "workerId": "w4", "name": "Worker 4", "skills": [] }
  ],
  "shifts": [
    { "date": "2026-03-01", "shiftType": 0, "requiredWorkers": 1, "preassignedWorkerIds": [], "requiredSkills": [] },
    { "date": "2026-03-02", "shiftType": 0, "requiredWorkers": 1, "preassignedWorkerIds": ["w1"], "requiredSkills": [] },
    { "date": "2026-03-03", "shiftType": 0, "requiredWorkers": 1, "preassignedWorkerIds": [], "requiredSkills": [] },
    { "date": "2026-03-04", "shiftType": 0, "requiredWorkers": 1, "preassignedWorkerIds": ["w2"], "requiredSkills": [] },
    { "date": "2026-03-05", "shiftType": 0, "requiredWorkers": 1, "preassignedWorkerIds": [], "requiredSkills": [] },
    { "date": "2026-03-06", "shiftType": 0, "requiredWorkers": 1, "preassignedWorkerIds": [], "requiredSkills": [] },
    { "date": "2026-03-07", "shiftType": 0, "requiredWorkers": 1, "preassignedWorkerIds": [], "requiredSkills": [] }
  ],
  "allowMovePreassigned": false,
  "availabilityByWorkerId": {
    "w1": [
      "2026-03-01:Morning",
      "2026-03-02:Morning",
      "2026-03-05:Morning"
    ],
    "w2": [
      "2026-03-01:Morning",
      "2026-03-04:Morning",
      "2026-03-06:Morning"
    ],
    "w3": [
      "2026-03-02:Morning",
      "2026-03-03:Morning",
      "2026-03-04:Morning",
      "2026-03-07:Morning"
    ],
    "w4": [
      "2026-03-03:Morning",
      "2026-03-05:Morning",
      "2026-03-06:Morning",
      "2026-03-07:Morning"
    ]
  }
}
