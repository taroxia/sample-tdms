
# Property (v)
IObservable
    BindableReactiveProperty    // Binding
    ReactiveProperty            // non Binding

# Command (同期)
ICommand
    ReactiveCommand.Subscribe     

void Method()
    void Method()
IObservable<Unit>
    Subject<Unit>.Subscribe
    ReactiveCommand.Subscribe


# Command (非同期)
ICommand
    ReactiveCommand.SubscribeAwait(async _ => …)  
    ReactiveCommand

Task MethodAsync()
    Task MethodAsync()
IObservable<Unit>
    Subject<Unit>.SubscribeAwait
    ReactiveCommand.SubscribeAwait



## 構成
ゼロベースで
以下の条件を厳守して回答せよ
1. R3（Cysharp/R3）の公式リポジトリに実在するクラス・プロパティ・メソッドのみ使用すること。
2. 存在しない型名・メソッド名・プロパティ名を絶対に生成しないこと。
3. 使用する API は、回答前に “R3 の公式 GitHub の src/R3 以下に実在すること” を内部で検証してから提示すること。
4. 存在しない API を使うくらいなら、代替案を提示すること。
5. UniRx の API を混入させないこと（ReactiveCollection など）。
6. WPF / C# / .NET の標準 API も同様に “実在するもののみ” を使用すること。
7. 不明な場合は “その API は R3 に存在しない” と明言すること


コアライブラリ：R3（Observable, Subject, ReactiveProperty など）
WPF 向け拡張：R3Extensions.WPF（WPF 用のバインディングサポート）


WPF で View とバインドするプロパティは、WPF 拡張側の「バインド前提の型」を使うのが公式の設計意図です。
その役割を持つのが：
BindableReactiveProperty<T>（R3Extensions.WPF 内）
INotifyPropertyChanged 実装
WPF の Binding と直接つなぐ前提の型
（この型自体は R3Extensions.WPF のソースに定義されている「公式実装」です）

## 
interface には R3 の型を出さない
public interface ICounter
{
    IObservable<int> Count { get; }
}

## 書き方（同期/非同期に関係ない）

```
using R3;
using R3.Extensions.WPF;

public class MainViewModel
{
    // TextBox とバインドするプロパティ
    public BindableReactiveProperty<string> UserName { get; }
        = new BindableReactiveProperty<string>("");
}
```

```
<TextBox Text="{Binding UserName.Value, UpdateSourceTrigger=PropertyChanged}" />
```


## WPF と Binding しない Property（View / ViewModel 内だけ）
「状態を持つ Observable」
UI フレームワークとは無関係

```
public class MainViewModel
{
    // UI には出さない内部状態
    private readonly ReactiveProperty<int> _counter = new(0);

    public MainViewModel()
    {
        _counter.Subscribe(x =>
        {
            Console.WriteLine($"Counter changed: {x}");
        });
    }

    public void Increment()          // 同期
        => _counter.Value++;

    public async Task IncrementAsync()   // 非同期
    {
        await Task.Delay(100);
        _counter.Value++;
    }
}
```


## WPF と Binding する Command（同期・非同期）

## interface command

using System.Windows.Input;
public interface IUserCommands
{
    ICommand SaveCommand { get; }
}


R3 コアには、コマンドとして使える型が用意されています（ReactiveCommand）。
WPF 拡張側で ICommand として WPF に橋渡しされます（R3Extensions.WPF）

ReactiveCommand
WPF の ICommand として使える
実行可否を IObservable<bool> から構成できる（拡張メソッド経由）

### 同期

```
    public BindableReactiveProperty<string> Name { get; } = new("");

    public ReactiveCommand GreetCommand { get; }

    public MainViewModel()
    {
        var canExecute =
            Name.Select(x => !string.IsNullOrWhiteSpace(x));

        // WPF 拡張側の ToReactiveCommand 拡張メソッド（R3Extensions.WPF）
        GreetCommand = canExecute.ToReactiveCommand();
        GreetCommand.Subscribe(_ => Greet());
    }

    private void Greet()
    {
        Console.WriteLine($"Hello, {Name.Value}");
    }
```

```
<Button Content="Greet" Command="{Binding GreetCommand}" />
```

### 非同期


R3 の設計思想は「単発の非同期処理は async/await に任せる」なので

コマンド型は ReactiveCommand のまま
非同期は SubscribeAwait で Task を正しく扱う
SubscribeAwait は、Rx と async/await の連携を安全に行うための公式 API として解説されていま

```
public class MainViewModel
{
    public BindableReactiveProperty<string> Query { get; } = new("");

    public ReactiveCommand SearchCommand { get; }

    public MainViewModel()
    {
        var canExecute =
            Query.Select(x => !string.IsNullOrWhiteSpace(x));

        SearchCommand = canExecute.ToReactiveCommand();

        // ★ 非同期は SubscribeAwait で書くのが R3 流
        SearchCommand.SubscribeAwait(async _ =>
        {
            await SearchAsync();
        });
    }

    private async Task SearchAsync()
    {
        await Task.Delay(500);
        // 非同期の検索処理
    }
}
```


## WPF と Binding しない Command（内部専用）

ReactiveCommand（UI に出さない用途でも使える）
Subject<Unit>


### interface
一番素直：

同期：void Method()
非同期：Task MethodAsync()

リアクティブに寄せるなら：
interface：IObservable<Unit>

### 同期の内部トリガ

```
using R3;

public class MainViewModel
{
    private readonly Subject<Unit> _refresh = new();

    public MainViewModel()
    {
        _refresh.Subscribe(_ => RefreshCore());
    }

    public void TriggerRefresh()
        => _refresh.OnNext(Unit.Default);

    private void RefreshCore()
    {
        // 内部の再計算処理など
    }
```

### 非同期の内部トリガ

```
public class MainViewModel
{
    private readonly Subject<Unit> _refresh = new();

    public MainViewModel()
    {
        _refresh
            .SubscribeAwait(async _ =>
            {
                await RefreshAsync();
            });
    }

    public void TriggerRefresh()
        => _refresh.OnNext(Unit.Default);

    private async Task RefreshAsync()
    {
        await Task.Delay(200);
        // 非同期の再計算処理
    }
}
```

