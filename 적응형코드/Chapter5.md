# Chapter5. 단일 책임 원칙

```
SOLID
- Single responsibility principle(단일 책임 원칙)
- Open-closed principle(개방/폐쇄 원칙)
- Liskov substitution principle(리스코프 치환 원칙)
- Interface segregation(인터페이스 분리)
- Dependency injection(의존성 주입)
```

단일 책임 원칙 - 오로지 한 가지 작업만 수행하며, 단 한 가지 이유에 의해서만 변경되는 코드를 작성하도록 권장하는 원칙

# 문제의 정의

[예제코드](source/AdaptiveCodeSolution/Chapter5/TradeProcessor/TradeProcessor.cs)

TradeProcessor 클래스의 책임  
- 스트림 읽기
- 문자열 파싱하기
- 필드 유효성 검사하기
- 로깅하기
- 데이터베이스에 삽입하기
  
## 명확성을 위한 리팩토링

```cs
public void ProcessTrades(Stream stream)
{
    var lines = ReadTradeData(stream);
    var trades = ParseTrades(lines);
    StoreTrades(trades);
}
```
_ 코드의 가독성이 향상됨

## 추상화를 위한 리팩토링
_ 모든 변경사항을 수용할 수 있도록 하기 위해 추상화를 적용

![그림5-1](assets/image/figure5-1.png)  

```cs
public class TradeProcessor
{
    public TradeProcessor(ITradeDataProvider tradeDataProvider, ITradeParser tradeParser, ITradeStorage tradeStorage)
    {
        this.tradeDataProvider = tradeDataProvider;
        this.tradeParser = tradeParser;
        this.tradeStorage = tradeStorage;
    }

    public void ProcessTrades()
    {
        var lines = tradeDataProvider.GetTradeData();
        var trades = tradeParser.Parse(lines);
        tradeStorage.Persist(trades);
    }

    private readonly ITradeDataProvider tradeDataProvider;
    private readonly ITradeParser tradeParser;
    private readonly ITradeStorage tradeStorage;
}
```
변경 후 TradeProcessor Class는   
_ 어떤 형식의 거래 데이터를 다른 형식으로 전환하는 프로세스를 모델링 => 책임   
_ 다른 책임에 대해서 수정할 필요가 없어짐  
  - 데이터를 Stream이 아닌 다른 곳에서 읽고 싶을 때 (ITradeDataProvider)  
  - 로그를 콘솔이 아닌 다른 곳에서 출력하고 싶을 때 (ITradeParser)
  - 거래 데이터를 다른 곳에 저장하고 싶을 때 (ITradeStorage)

### TradeDataProvider

```cs
public class StreamTradeDataProvider : ITradeDataProvider
{
    public StreamTradeDataProvider(Stream stream)
    {
        this.stream = stream;
    }

    public IEnumerable<string> GetTradeData()
    {
        var tradeData = new List<string>();
        using (var reader = new StreamReader(stream))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                tradeData.Add(line);
            }
        }
        return tradeData;
    }   

    private Stream stream;
}
```
- 데이터를 다른 곳에서 읽어오고 싶을 때만 변경
- `ITradeDataProvider`는 Stream 클래스에 의존하지 않음
- 인터페이스를 정의하고 추상화를 적용하는 방향으로 리팩토링할 때는 코드의 적응성에 영향을 미치는 의존성을 남겨두지 않는 것이 중요하다.
- Stream이 아닌 다른 곳에서 데이터를 읽어 올 수 있다.
- `TradeProcessor`는 오로지 `ITradeDataProvider`를 통해서 `GetTradeData` 메서드의 시그니처에 대해서만 알 수 있다.

### TradeParser

![그림5-2](assets/image/figure5-2.png)  


- 유효성 검사와 매핑 기능을 하는 책임도 별개의 클래스로 분리하여 하나의 책임만 하도록 수정
- 거래 데이터의 형식이 변경되는 경우에만 수정

```cs
public class SimpleTradeParser : ITradeParser
{
    private readonly ITradeValidator tradeValidator;
    private readonly ITradeMapper tradeMapper;

    public SimpleTradeParser(ITradeValidator tradeValidator, ITradeMapper tradeMapper)
    {
        this.tradeValidator = tradeValidator;
        this.tradeMapper = tradeMapper;
    }

    public IEnumerable<TradeRecord> Parse(IEnumerable<string> tradeData)
    {
        var trades = new List<TradeRecord>();
        var lineCount = 1;
        foreach (var line in tradeData)
        {
            var fields = line.Split(new char[] { ',' });

            if (!tradeValidator.Validate(fields))
            {
                continue;
            }

            var trade = tradeMapper.Map(fields);

            trades.Add(trade);

            lineCount++;
        }

        return trades;
    }
}
```

### TradeStorage

- 로깅 기능을 추상화!! 

![그림5-3](assets/image/figure5-3.png)  

- `Log4NetLoggerAdapter`   
  - 어댑터 클래스로 만들어서 third-party를 first-party 참조로 변경
- `ILogger`
  - AdoNetTradeStorage, SimpleTradeValidator가 참조
  - 런타임에는 Log4Net 라이브러리 의존
  - Log4Net 라이브러리에 대한 참조는 애플리캐이션 진입점과 새로 생성하는 Service.Log4Net


# SRP와 데코레이터 패턴

> **데코레이터 패턴** - 개별 클래스가 단일 책임을 가질 수 있도록 하기에 매우 적합한 패턴

![그림5-4](assets/image/figure5-4.png)  

## 컴포지트 패턴
> **컴포지트 패턴** - 데코레이터 패턴에서 파생한 패턴으로서 데코레이터 패턴을 사용하는 일반적인 패턴 중 하나

![그림5-5](assets/image/figure5-5.png)  

**목적**
- 여러 인터페이스의 인스턴스를 마치 하나의 인스턴스인 것처럼 취급하는 것이다.
- 그래서 클라이언트는 단 하나의 인스턴스만을 받아들인 후 별도의 수정 없이도 해당 인스턴스를 여러 개의 인스턴스처럼 활용할 수 있는 방법이다.

- 이 패턴을 이용하면 하나 그 이상의 CompositeComponent객체를 AddComponent 메서드에 전달하여 상속 구조를 표현하는 트리 형태의 인스턴스를 조합하여 연결할 수 있다.

![그림5-6](assets/image/figure5-6.png)  

## 조건부 데코레이터(Predicate Decorator)
> **조건부 데코레이터** - 코드가 조건부로 실행되는 과정을 클라이언트에게 숨기고자하는 경우에 유용

![UML5-1](assets/image/uml5-1.png)  

- `TodayIsAnEvenDayOfTheMonthPredicate` - 어댑터 패턴

## 분기 데코레이터(Branching Decorator)
> 조건부 데코레이터에 약간의 수정을 가하면 데코레이트된 인터페이스를 받아들여 조건식의 결과가 거짓인 경욷에도 뭔가를 실행할 수 있도록 변경할 수 있다.

```cs
public class BranchedComponent : IComponent
{
    public BranchedComponent(
        IComponent trueComponent, 
        IComponent falseComponent,           
        IPredicate predicate)
    {
        this.trueComponent = trueComponent;
        this.falseComponent = falseComponent;
        this.predicate = predicate;
    }

    public void Something()
    {
        if (predicate.Test())
        {
            trueComponent.Something();
        }
        else
        {
            falseComponent.Something();
        }
    }

    private readonly IComponent trueComponent;
    private readonly IComponent falseComponent;
    private readonly IPredicate predicate;
}
```

## 지연 데코레이터(Lazy Decorator)
> 지연 데코레이터 - 클라이언트가 실제로 해당 인스턴스를 사용하기 전까지는 객체의 인스턴스를 생성하지 않는 기능을 제공하기 위한 것

```cs
// 클라이언트는 객체가 지연 생성될 것임을 인지할 수 밖에 없다.
public class ComponentClient
{
    public ComponentClient(Lazy<IComponent> component)
    {
        this.component = component;
    }

    public void Run()
    {
        component.Value.Something();
    }

    private readonly Lazy<IComponent> component;
}
```

```cs
public class LazyComponent : IComponent
{
    public LazyComponent(Lazy<IComponent> lazyComponent)
    {
        this.lazyComponent = lazyComponent;
    }

    public void Something()
    {
        lazyComponent.Value.Something();
    }
    
    private readonly Lazy<IComponent> lazyComponent;
}

// 인터페이스를 활용
// 클라이언트는 객체가 지연생성될 거라 인지하지 못 함
public class ComponentClient
{
    public ComponentClient(IComponent component)
    {
      this.component = component;
    }
    
    public void Run()
    {
        component.Something();
    }
    
    private readonly IComponent component;
}
```

## 로깅 데코레이터(Logging Decorator)
> 