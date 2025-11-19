# Tech Challenge DnD-1 - Sensor Data Processor

## Overview

High-performance solution built with .NET 9.0 for processing sensor data and generating optimized output reports in multiple formats (CSV, XML). The application leverages modern concurrency patterns and streaming techniques to handle large datasets efficiently.

## Solution Architecture

### Projects

**TechChallenge.Application**: Core challenge solution
- Implements sensor data processing with clean architecture patterns
- Includes optimized processors for CSV and XML with streaming support
- Task-based concurrent processing for multiple output formats

**TechChallenge.Application.Tests**: Comprehensive unit test suite
- Performance tests for large datasets
- Integration tests for file processing workflows

**TechChallenge.Client**: Demo and testing client
- Console application to execute the complete solution
- Configuration via `process-config.json`

## Key Features

### Data Processing
- **Streaming reads**: Processes large JSON files without loading everything into memory
- **Robust validation**: File, path, and input format verification
- **Concurrent output generation**: Parallel processing of multiple output formats using task-based concurrency

### Optimized Output Formats

#### Custom CSV Format
- **Two-section format**:
  1. Global metrics (max sensor ID, global average)
  2. Detailed zone information (name, average, active sensors)
- **Custom separator**: Uses semicolon (`;`) for international compatibility

#### Structured XML
- **Optimized serialization**: Uses XmlSerializer with DTOs
- **Clean hierarchy**: Well-structured XML schema

## Concurrency Architecture: Thread vs Task

### Design Decision

**To evaluate both alternatives and their performance characteristics, I implemented 2 options: one using traditional threads with BlockingCollection, and another using modern tasks with channels. This allows for direct performance comparison between the two approaches.**

The application provides a runtime choice between:
- **Task-based handler**: Modern async/await patterns with channels
- **Thread-based handler**: Traditional threading with BlockingCollection

### Performance Evaluation

When you run the application, it will:
1. **Prompt you to choose** between Task or Thread implementation
2. **Display processing time** for performance comparison
3. **Show detailed metrics** including duration and throughput

### The Conceptual Distinction

To make the right architectural decision, you must understand the hierarchy of abstractions:

#### `Thread` (System.Threading.Thread)
* **Low-Level Abstraction**: Represents an actual Operating System thread (Windows/Linux)
* **High Resource Cost**: Creating a thread is expensive (~**1MB of stack memory** per thread) with OS kernel overhead
* **Context Switching**: The OS must schedule these threads. Too many dedicated threads lead to thread starvation and excessive context switching
* **Foreground by Default**: Keeps the application process alive even if main execution finishes

#### `Task` (System.Threading.Tasks.Task)
* **High-Level Abstraction**: Represents a unit of work (a "Promise" or "Future"), **not** a thread itself
* **Lightweight**: Small object managed by the runtime
* **ThreadPool Backed**: Tasks are queued and executed by the .NET ThreadPool with intelligent "Hill Climbing" algorithm
* **Composable**: Native support for chaining, combining (`WhenAll`), and the `async/await` state machine

### Summary Checklist

- [x] **CPU-Bound Work**: Use `Task.Run()`
- [x] **I/O-Bound Work**: Use pure `async`/`await` (do not wrap in Task.Run)
- [x] **Long Running Background Work**: Use `Task.Factory.StartNew(..., TaskCreationOptions.LongRunning)`
- [x] **Never** use `new Thread()` with `async` delegates
- [x] **Parallel Processing**: Use `Task.WhenAll()` for concurrent operations

## Configuration and Usage

### Configuration File (`process-config.json`)

```json
{
  "InputFilePath": "Input/sensores.json",
  "OutputRequests": [
    {
      "OutputFilePath": "Output/CSV",
      "OutputType": "Csv"
    },
    {
      "OutputFilePath": "Output/XML",
      "OutputType": "Xml"
    }
  ]
}
```

### Execution

The application will prompt you to choose between Task-based or Thread-based processing, then display processing times for performance comparison.

```bash
cd src/TechChallenge.Client
dotnet run
```

**Example output:**

```txt
=== Sensor Output Handler Selection ===
Choose the output processing implementation:
1. Task-based handler (Modern, async/await, better performance)
2. Thread-based handler (Traditional, blocking operations)
Enter your choice (1 or 2) [Default: 1]: 2
Selected: Task-based handler

Processing completed successfully!

Processed sensor data. Using 'Threads':
---------------------
Start: 06:41:51 PM -3
End: 06:41:56 PM -3
Duration: 00:00:05.2719248
Total inputs: 750000
Active inputs: 525231
Max Value Sensor ID: d1cfa949-a09f-4e24-9097-36a455478202
Max Value Sensor ID: d1cfa949-a09f-4e24-9097-36a455478202
Global Average Value: 26041.66
Zone Information Count: 5
        Zone Z01: Avg=26020.34, Active Sensors=104973
        Zone Z02: Avg=26090.46, Active Sensors=104913
        Zone Z03: Avg=25993.91, Active Sensors=105030
        Zone Z04: Avg=26030.68, Active Sensors=105193
        Zone Z05: Avg=26072.91, Active Sensors=105122

```

```txt
=== Sensor Output Handler Selection ===
Choose the output processing implementation:
1. Task-based handler (Modern, async/await, better performance)
2. Thread-based handler (Traditional, blocking operations)
Enter your choice (1 or 2) [Default: 1]: 1
Selected: Task-based handler

Processing completed successfully!

Processed sensor data. Using 'Tasks':
---------------------
Start: 06:44:09 PM -3
End: 06:44:14 PM -3
Duration: 00:00:05.1685814
Total inputs: 750000
Active inputs: 525231
Max Value Sensor ID: d1cfa949-a09f-4e24-9097-36a455478202
Max Value Sensor ID: d1cfa949-a09f-4e24-9097-36a455478202
Global Average Value: 26041.66
Zone Information Count: 5
        Zone Z01: Avg=26020.34, Active Sensors=104973
        Zone Z02: Avg=26090.46, Active Sensors=104913
        Zone Z03: Avg=25993.91, Active Sensors=105030
        Zone Z04: Avg=26030.68, Active Sensors=105193
        Zone Z05: Avg=26072.91, Active Sensors=105122
```

### Sensor Data Generator

The application includes a built-in sensor data generator to create test files with customizable parameters.

**Usage:**
```bash
# Generate a sensor file with specified parameters
dotnet run generate <zones> <itemsPerZone> <outputFile>

# Examples:
dotnet run generate 5 1000 "./Input/sensores-small.json"     # 5K records (5 zones × 1K each)
dotnet run generate 3 10000 "./Input/sensores-medium.json"   # 30K records (3 zones × 10K each)
dotnet run generate 7 50000 "./Input/sensores-large.json"    # 350K records (7 zones × 50K each)
```

**Parameters:**
- `<zones>`: Number of zones to generate (Z01, Z02, etc.)
- `<itemsPerZone>`: Number of sensor records per zone
- `<outputFile>`: Full path to output JSON file (including directory)

**Generated data characteristics:**
- **Sensor IDs**: Random GUIDs for realistic testing
- **Values**: Random values between 2,000-50,000 (simulating real sensor ranges)
- **Zones**: Distributed across specified number of zones (Z01, Z02, etc.)
- **Active status**: ~70% of sensors are active (realistic operational ratio)
- **JSON format**: Lowercase property names matching the application's expected format

**Example generated file structure:**
```json
[
  {"index": 1, "id": "550e8400-e29b-41d4-a716-446655440000", "value": 25847.3, "zone": "Z01", "isActive": true},
  {"index": 2, "id": "6ba7b810-9dad-11d1-80b4-00c04fd430c8", "value": 31245.7, "zone": "Z02", "isActive": false}
]
```

### Running Tests

```bash
cd src
dotnet test
```

## Technologies and Dependencies

- **.NET 9.0**: Primary framework
- **System.Text.Json**: JSON serialization with streaming support
- **System.IO.Abstractions**: Testable file system abstraction
- **xUnit + FluentAssertions**: Modern testing framework
- **NSubstitute**: Mocking library for unit tests

## Performance Characteristics

### Memory Efficiency
- Streaming JSON parsing avoids loading entire file into memory
- Task-based concurrency minimizes thread overhead
- Efficient buffer management for large datasets

### Concurrency Benefits
- Parallel output generation reduces overall processing time
- ThreadPool optimization handles workload efficiently
- No manual thread management overhead

### Scalability
- Can handle thousands of sensor records
- Multiple output formats processed concurrently
- Minimal memory footprint even with large inputs

## Best Practices Applied

- **SOLID Principles**: Single responsibility, dependency injection
- **Async/Await**: Non-blocking I/O operations
- **Task-Based Concurrency**: Modern .NET concurrency patterns
- **Streaming**: Memory-efficient large file processing

