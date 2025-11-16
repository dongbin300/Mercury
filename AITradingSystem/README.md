# AI Trading Strategy Automation System

이 시스템은 다음 과정을 자동화합니다:

1. **전략 코딩**: 다양한 거래 전략 자동 생성
2. **백테스팅 실행**: Backtester2를 사용하여 자동 테스트
3. **결과 분석**: 성과 지표 분석 및 파라미터 중요도 계산
4. **전략 개선**: 유전 알고리즘 기반 전략 최적화

## 시스템 구조

```
AITradingSystem/
├── Models/
│   └── StrategyInfo.cs          # 데이터 모델
├── AutoTradingPipeline.cs      # 메인 파이프라인
├── StrategyGenerator.cs        # 전략 생성기
├── BacktestRunner.cs          # 백테스팅 실행기
├── ResultAnalyzer.cs          # 결과 분석기
├── StrategyImprover.cs        # 전략 개선기
├── Program.cs                 # 메인 프로그램
└── README.md
```

## 사용 방법

### 1. 시스템 빌드

```bash
cd D:\Project\CS\Mercury\AITradingSystem
dotnet build --configuration Release
```

### 2. 자동화 실행

```bash
# 기본 실행 (10회 반복)
dotnet run

# 사용자 지정 반복 횟수
dotnet run 20
```

### 3. 실행 결과 확인

실행이 완료되면 다음 경로에서 결과를 확인할 수 있습니다:

```
AITradingSystem/
├── Strategies/               # 생성된 전략 코드
├── Backtests/               # 백테스팅 결과
├── Results/                 # 분석 결과
├── Improvements/            # 개선 이력
└── strategies_metadata.json # 전략 메타데이터
```

## 주요 기능

### 자동 전략 생성

- **CCI 전략**: Commodity Channel Index 기반
- **EMA 전략**: Exponential Moving Average 기반
- **MACD 전략**: MACD 지표 기반
- **RSI 전략**: Relative Strength Index 기반
- **하이브리드 전략**: 여러 지표 조합

### 파라미터 최적화

- 진입/청산 조건 자동 튜닝
- 리스크 관리 파라미터 최적화
- 볼륨 확인 조건 추가
- 멀티타임프레임 분석

### 성과 분석

- **ROI (Return on Investment)**: 수익률
- **승률**: 수익 거래 비율
- **MDD (Maximum Drawdown)**: 최대 손실률
- **Risk/Reward Ratio**: 리스크 대비 수익률

### 자동 개선

- 파라미터 중요도 분석
- 유전 알고리즘 기반 최적화
- 논리 기반 전략 개선
- 리스크 관리 강화

## 설정 옵션

### 기본 설정

- **테스트 심볼**: BTCUSDT, ETHUSDT, ADAUSDT, DOTUSDT, LINKUSDT
- **타임프레임**: 1시간, 4시간, 일봉
- **테스트 기간**: 2023년 1월 1일 - 2023년 12월 31일
- **레버리지**: 10배
- **최대 포지션**: 5개
- **수수료**: 0.02%

### 최적화 중단 조건

- ROI 30% 이상 AND 승률 60% 이상인 전략 발견 시
- 최대 반복 횟수 도달 시
- 사용자 중단 (Ctrl+C)

## 출력 결과

### 전략 성과 순위

각 반복(iteration)별로 상위 전략이 순위매겨져 출력됩니다:

1. 전략 이름과 유형
2. 평균 ROI, 승률, MDD
3. 리스크 대비 수익률
4. 총 거래 횟수
5. 최적화된 파라미터

### 개선 추천

분석 결과를 바탕으로 다음과 같은 개선 사항을 추천합니다:

- 진입/청산 조건 조정
- 추가 확인 지표 사용
- 리스크 관리 강화
- 파라미터 튜닝 방향

## 주의사항

1. **백테스팅은 과거 데이터 기반**: 실제 거래와는 차이가 있을 수 있습니다
2. **시장 조건 변화**: 과거 성과가 미래 성과를 보장하지 않습니다
3. **리스크 관리**: 항상 적절한 리스크 관리가 필요합니다
4. **실제 투자 전 충분한 검증**: 데모 거래 등을 통한 검증이 필요합니다

## 기술적 세부사항

### 의존성

- .NET 10.0
- Binance.Net (거래소 데이터)
- Mercury 백테스팅 프레임워크
- System.Text.Json (데이터 직렬화)

### 성능

- 병렬 백테스팅 (CPU 코어 수만큼)
- 메모리 효율적 데이터 처리
- 자동 임시 파일 정리

### 확장성

- 새로운 전략 템플릿 추가 용이
- 커스텀 분석 지표 추가 가능
- 다양한 거래소 지원 확장 가능

## 지원 및 문제 해결

시스템 실행 중 문제가 발생하면 다음을 확인하세요:

1. **Backtester2.exe 경로**: `D:\Project\CS\Mercury\Backtester2\bin\Debug\net10.0-windows7.0\`
2. **.NET 10.0 설치**: 최신 .NET 런타임 설치 확인
3. **디스크 공간**: 충분한 디스크 공간 확보
4. **권한**: 프로젝트 폴더에 쓰기 권한 확인

## 라이선스

이 시스템은 개인 연구 및 교육 목적으로 사용할 수 있습니다. 실제 투자에 사용하기 전에는 충분한 검증이 필요합니다.