# NetworkTest

## 한국어

NetworkTest는 IP 주소 또는 호스트의 네트워크 연결 상태를 지속적으로 확인하는 Windows 데스크톱 프로그램입니다. 일반 Ping(ICMP) 테스트와 특정 서비스 포트의 TCP 연결 테스트를 지원합니다.

### 주요 기능

- IP 또는 호스트만 입력하면 ICMP Ping 응답시간을 측정합니다.
- 포트를 입력하면 해당 TCP 포트까지 연결되는 시간을 측정합니다.
- 시작과 종료 버튼으로 주기적인 측정을 제어합니다.
- 측정 시각(밀리초 포함), 방식, 성공·실패, 응답시간, 상세 정보를 Grid에서 확인합니다.
- 총 횟수, 성공 횟수, 실패 횟수를 가로 막대 그래프로 보여 줍니다.
- 각 측정 간격의 응답시간을 Windows 작업 관리자와 유사한 실시간 흐름 그래프로 표시합니다.
- 새 테스트를 시작하면 이전 화면 데이터를 초기화합니다.
- 테스트 시작부터 종료까지를 하나의 CSV 로그 파일로 저장합니다.
- 로그에는 대상 IP/호스트, 포트 또는 ICMP 방식, 측정 간격, 시작·종료 시각 및 모든 결과가 포함됩니다.

### 사용 방법

1. 대상 IP 주소 또는 호스트 이름을 입력합니다.
2. Ping 테스트는 포트를 비워 두고, 특정 서비스 연결 확인은 포트를 입력합니다.
3. 측정 간격(초)을 입력한 후 **시작**을 누릅니다.
4. 테스트를 중지하려면 **종료**를 누릅니다.

측정 로그는 실행 파일 옆 `Logs` 폴더에 저장됩니다.

---

## English

NetworkTest is a Windows desktop application for continuously monitoring the network connectivity of an IP address or host. It supports standard ICMP Ping tests and TCP connection tests for a specified service port.

### Features

- Measures ICMP Ping response time when only an IP address or host is supplied.
- Measures TCP connection time when a port is supplied.
- Controls periodic measurements with Start and Stop buttons.
- Displays timestamps including milliseconds, method, success/failure state, response time, and details in a results grid.
- Shows total, successful, and failed measurements in a horizontal bar chart.
- Displays each interval's response time in a live, Windows Task Manager-style timeline graph.
- Clears previous on-screen data when a new test begins.
- Saves each start-to-stop test session as a separate CSV log file.
- Logs the target IP/host, selected port or ICMP mode, interval, session start/end times, and all results.

### Usage

1. Enter the target IP address or host name.
2. Leave the port blank for Ping, or enter a port to test a specific TCP service.
3. Enter the measurement interval in seconds and select **Start**.
4. Select **Stop** to end the test.

Measurement logs are saved in the `Logs` folder next to the application executable.
