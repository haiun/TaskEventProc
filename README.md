# TaskEventProcessor
Task기반의 실시간 게임의 로직을 Unity클라이언트와 .Net서버를 통해 실시간으로 동기화 시스템을 구현했을 때, 메세지풀링을 하지 않으면서 높은 반응성을 구현하기 위해서 구현했던 결과를 기록합니다.
주요 아이디어는 TaskEventConsumer(https://github.com/haiun/TaskEventProc/blob/main/Assets/Scripts/TaskEvent/TaskEventConsumer.cs)에 구현되어있습니다.
이벤트를 수집하는 과정에서 ConcurrentQueue와 eventOverlaps(이벤트충첩수)변수에 대한 Interlocked연산을 통해 필요할때에만 작업을 풀링하며 즉시 처리합니다.
