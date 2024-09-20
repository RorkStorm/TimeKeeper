# TimeKeeper

TimeKeeper is a Windows service that manages user sessions and enforces session time limits. It uses Topshelf for service management and NLog for logging.

## Features

- Monitors user sessions and logs session changes.
- Enforces session time limits for specified users.
- Logs off users when their session time limit is reached.
- Configurable via `appsettings.json`.

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or later (optional, for development)
- Windows operating system

### Compilation

1. Clone the repository:
    ```sh
    git clone https://github.com/yourusername/TimeKeeper.git
    cd TimeKeeper
    ```

2. Build the project:
    ```sh
    dotnet build
    ```

3. Run the project:
    ```sh
    dotnet run
    ```

### Configuration

The service is configured using the `appsettings.json` file. Here is an example configuration:

```json
{
  "TimeCounters": {
    "User1": "60",
    "User2": "120"
  }
}
```

- `TimeCounters`: A dictionary where the key is the username and the value is the default session time limit in minutes.

## Usage

- Start the service:
    ```sh
    TimeKeeper.exe start
    ```
- Stop the service:
    ```sh
    TimeKeeper.exe stop
    ```
- Install the service:
    ```sh
    TimeKeeper.exe install
    ```
- Uninstall the service:
    ```sh
    TimeKeeper.exe uninstall
    ```

## Logging

The service uses NLog for logging. The logging configuration is specified in the `NLog.config` file. By default, logs are written to a file named `TimeKeeper.log`.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.

## License

This project is licensed under the MIT License. See the `LICENSE` file for details.

## Acknowledgements

- **Topshelf** - An easy service hosting framework for building Windows services using .NET.
- **NLog** - A logging framework for .NET.

## Contact

For any questions or suggestions, please contact [Eric F](mailto:eric@fonteyne.net).
