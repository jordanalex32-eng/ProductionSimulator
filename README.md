# Production Line Simulator (C# WinForms)

This repository contains a **C# Windows Forms application** that simulates a simple production / conveyor line.  
It’s primarily a practice project for **multithreaded programming, semaphores, and UI-driven simulation**.

Each “X” on the layout represents a pallet/part moving through the line. Movement between stations is controlled by semaphores so that pallets compete for shared resources the way they would on a real line.

> 🔗 GitHub repo: https://github.com/jordanalex32-eng/ProductionSimulator  

---

## High-Level Overview

- Simulates a **conveyor line** with multiple stations (load, testers, unload).
- Each pallet is modeled as a **worker object on its own thread**.
- **Semaphores** coordinate access to stations so pallets:
  - Wait if a station is occupied.
  - Reroute if another path is available.
  - Enter a **blocked/fault state** when no valid path is available.
- The UI shows the line as a grid of “slots” – colored rectangles/X’s – that light up as pallets move.
- **Stopwatches and counters** collect simple metrics (production time, station downtime, OK output, etc.).

The goal is to visualize how concurrency, blocking, and routing decisions affect throughput and downtime.

---

## Main Features

### Simulation Controls

- **Start** – begins the simulation using the current time and pass-rate parameters.
- **Stop** – pauses the simulation while keeping the current state.
- **Reset** – clears the layout, timers, and counters back to an initial state.
- **Simulate Break Time** – triggers a planned break/stop event to see the effect on throughput.
- **Sim Speed %** – adjusts simulated speed:
  - `100%` speed is intentionally **faster than real time** (about 10% of actual process time),  
    so the line appears to run “sped up” while still preserving relative timing.

### Simulation Parameters

Examples of configurable parameters:

- LV / HV / NV test pass percentages
- Initial station pass %
- Break-in pass %
- SRFT for output count %
- Number of pallets online
- Current seats run
- OK output

Default values are pre-filled so a user can just press **Start** and watch the simulation.

### Time Parameters

Timing values (in milliseconds) for:

- Tester times per station
- Break-in time
- Load / unload station times
- Pallet index time
- Transfer time
- Retry / ON-retries timing

These determine how long pallets occupy each station, how long blocking lasts, and how throughput behaves.

### Line Visualization

- The right side of the form shows a **grid of rectangles (“slots”)** representing:
  - Testers (Tester 1, Tester 2, Tester 3)
  - Load station
  - Unload station
  - Intermediate conveyor positions
- Slot colors indicate state, for example:
  - **Green** – OK / successfully tested
  - **Yellow/Orange** – in process or waiting
  - **Red** – blocked/faulted (no valid path or collision)

If a pallet can’t move forward because of another pallet or a busy station:

1. A **decision tree** evaluates alternate paths (e.g., route to a different tester).
2. If no route is possible, the pallet goes into a blocked state and waits or faults out.
3. Corresponding stopwatches and counters update so the effect is visible.

### Metrics & Stopwatches

The UI includes several stopwatch fields:

- Total production stopwatch
- Individual tester stopwatches
- Load-station wait time
- Other simple utilization/idle timers

These update live as the simulation runs.

---

## How It Works (Technical Summary)

- **Platform:** C# WinForms
- **Concurrency:** Multiple worker threads (one per pallet/seat) using `System.Threading`.
- **Synchronization:**  
  - **Semaphores** (`Semaphore` / `SemaphoreSlim`) gate access to shared stations.
- **Routing logic:**  
  - Simple decision tree checks for available tester paths.
  - If a tester is free, the pallet claims the semaphore and moves.
  - If all paths are blocked, the pallet transitions to a blocked state and the UI shows a fault.
- **Timing:**  
  - Processing and blocking durations are driven by user-configurable millisecond values.
  - Simulation speed scaling (Sim Speed %) multiplies these values to run faster than real time.
- **UI updates:**  
  - Thread-safe updates to WinForms controls change colors, counters, and stopwatch values.

The focus is on **core concurrency and visualization**, not external IO or persistence.

---

## Getting Started

### Prerequisites

- Windows
- **Visual Studio** (2022 or similar) with:
  - “.NET desktop development” workload installed
- Classic **.NET Framework** (see `ProductionSimulator.csproj` for the exact version)

### Clone and Run

1. Clone the repository:

   ```bash
   git clone https://github.com/jordanalex32-eng/ProductionSimulator.git
   cd ProductionSimulator
Open ProductionSimulator.sln in Visual Studio.

Set ProductionSimulator (WinForms project) as the Startup Project.

Press F5 (Start Debugging) to build and run.

How to Use the Simulator
Launch the application.

Review Simulation Parameters and Time Parameters; defaults are already populated.

Adjust Sim Speed % if you want a faster or slower animation.

Click Start to begin the simulation.

Watch pallets (X’s/blocks) move through the line:

Colors change as they are tested, blocked, or completed.

Use Stop to pause, and Reset to clear everything and start over.

Try changing:

Tester times

Pass percentages

Break times

…to see how small parameter changes impact congestion and throughput.

Possible Extensions
Persist metrics to a database or CSV for analysis.

Add charts for throughput and station utilization.

Support multiple line configurations from configuration files.

Add basic optimization to auto-tune timing parameters.

License
This project is currently intended for personal learning and demonstration.
Feel free to adapt or extend it for educational use. To open-source it formally, add a license
file (e.g., MIT or Apache 2.0) and update this section accordingly.
