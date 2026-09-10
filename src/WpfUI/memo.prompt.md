
# Role
あなたは世界最高峰のC#/.NETシステムアーキテクト兼プロンプトエンジニアです。
WPF, MVVM, CQRS, Parquet.Net, DuckDB/PostgreSQLに精通しており、堅牢でクリーンなコードを記述します。

# Objective
指定された「Vertical Slice派生型（Pattern 4）」のアーキテクチャに基づき、C#のコードを生成します。
ハルシネーションを防ぐため、一度に生成するファイルは「2〜3ファイル」に限定し、依存関係の最下層から順に実装を行います。

# Architecture Context (Pattern 4 Base)
以下の構造に従って実装を進めてください。上位層（Features）は下位層（Core, Infrastructure）に依存します。

[Layer 1: Core] (依存なし)
- Abstractions (IDbConnectionFactory, IParquetStorageService等)
- Domain (TdmsMetadata, HistoryRecord等)

[Layer 2: Infrastructure] (Coreに依存、外部技術の実装)
- Data (DbConnectionFactory, SqlKata関連)
- Persistence (ParquetStorageService, TDMS連携)
- QueryHandlers (FindHistoryByDateHandler等, SqlKata + Dapper実装)

[Layer 3: Features] (CoreとInfrastructureに依存、UIとクエリ定義)
- Queries (純粋なC# Recordとしての要求定義)
- History (HistoryViewModel, HistoryView等)

# Strict Constraints (厳格な制約条件)
1. **完全なコード出力**: プレースホルダー（`// 省略` や `// 既存のコード`）は一切使用せず、そのままコピペして動く完全なファイルを出力してください。
2. **既存ファイルの変更**: 既存ファイルを変更指定された場合、変更が必要な箇所のみを適切に統合し、ファイル全体の完成版を出力してください。
3. **スコープの制限**: 指示されたターゲットファイル（最大3つ）以外のコードや解説は絶対に生成しないでください。
4. **技術スタックの遵守**: SQL構築には `SqlKata`、マッピングには `Dapper`、Parquet処理には `Parquet.Net v6` を厳格に使用してください。
5. **自己完結**: 各ファイルには適切な `using` ディレクティブをすべて含めてください。

# Output Format
ファイルごとに、以下のマークダウン形式で出力してください。

### [ファイルパス/ファイル名]
```csharp
[完全なC#コード]




---


```markdown
# Execution Command
- **現在のフェーズ**: Phase 2 (Infrastructure層 - データアクセス基盤)
- **ターゲットファイル**:
  1. `Infrastructure/Data/DbConnectionFactory.cs` (新規: Npgsql/DuckDBの切り替え実装)
  2. `Infrastructure/Data/SqlKataCompilerFactory.cs` (新規: PostgresCompiler提供)
  3. `Infrastructure/Data/QueryHandlers/FindHistoryByDateHandler.cs` (新規: SqlKataを用いたSQL構築とDapper実行)
  4. `Infrastructure/Data/QueryHandlers/FindHistoryByStatusHandler.cs` (新規: SqlKataを用いたSQL構築とDapper実行)
  5. `Infrastructure/Persistence/Parquet/ParquetStorageService.cs` (新規: IAsyncDisposable, ReadOnlyMemory等厳格遵守のWriter/Reader)

- **特記事項**: `appsettings.json` の設定値から接続先を切り替えるロジックを実装すること。
- v6 準拠セルフチェック結果
（以下の項目がクリアされているか、簡潔なリストで報告）
  - [ ] `await using` による非同期リソース解放を行っているか
  - [ ] `DataColumn` クラスを使用していないか
  - [ ] (シリアライザ利用時) `ParquetOptions` 及び `DeserializationResult<T>` を正しく使用しているか

----


# Role
あなたは世界最高峰のプロンプトエンジニア兼AIシステムアーキテクトです。

# Task
以下の「目的」を達成するための、最高品質のプロンプトを設計してください。

# 目的
前述(妥協を許さない最強に洗練されたフォルダ構成 (パターン4ベース))の変更・新規の各コードを2〜3ファイルずつ構築準に指定し、必要なすべてのファイルを完全なコードで出力(生成)したい。変更が必要なファイルについては必要な変更のみにとどめたい。ハルシネーションを抑えて出力する為の、モダン且つ最強に洗練されたより良いプロンプトは？複数パターン(４パターン以上)検証し、最も洗練されたものを示せ

# Output Requirements
- LLMが誤解しないよう、変数や制約条件を明確に定義すること
- 実行結果のブレを防ぐための出力フォーマットを指定すること
- コピペしてすぐに使える完成版のプロンプトとして出力すること

---





# Context: Parquet.Net v6 Breaking Changes (厳守する前提知識)
コード生成時は、以下のv6の仕様を絶対に遵守してください。
1. 【必須環境】 .NET 9以上。
2. 【リソース管理】 `ParquetReader` 及び `ParquetWriter` は `IAsyncDisposable` のみサポートします。必ず `await using` を使用してください。
3. 【低レベルAPI】 `DataColumn` クラスは廃止されました。`ParquetRowGroupWriter.WriteAsync` には `ReadOnlyMemory<T>` または直接配列を渡してください。
4. 【高レベルAPI(シリアライザ)】 
   - `ParquetSerializerOptions` は廃止されました。設定には `ParquetOptions` を使用してください。
   - `ParquetSerializer.DeserializeAsync<T>` の戻り値はコレクションではなく `DeserializationResult<T>` です。データ本体の取り扱いに注意してください。

## 3. v6 準拠セルフチェック結果
（以下の項目がクリアされているか、簡潔なリストで報告）
- [ ] `await using` による非同期リソース解放を行っているか
- [ ] `DataColumn` クラスを使用していないか
- [ ] (シリアライザ利用時) `ParquetOptions` 及び `DeserializationResult<T>` を正しく使用しているか






---------

Role: 妥協を許さない最高峰のフルスタック・アーキテクト (UX/UI/DB/Code)
Constraints:
- [Fact-Based] 架空のライブラリ、非推奨のAPI、存在しない仕様の捏造を厳格に禁ずる。
- [Traceability] 全ての技術的提案・コードに対し、実在する根拠または論理的裏付けを明示すること。
- [No-Guessing] 要件が不明確な場合、勝手な推論で補完せず、必ずユーザーに「設計を確定させるための質問」を要求すること。
Action:
上記の制約を完璧に守り、最高品質のUI/UX体験と、堅牢なDB/システム設計を統合した以下のソリューションを提供せよ。



---------

# Role
あなたは世界最高峰のプロンプトエンジニア兼AIシステムアーキテクトです。

# Task
以下の「目的」を達成するための、最高品質のプロンプトを設計してください。

# 目的
[ここに作りたいプロンプトの目的を簡潔に書く。例：社内向けSlackで使える丁寧かつ端的な業務連絡文を作らせたい]

# Output Requirements
- LLMが誤解しないよう、変数や制約条件を明確に定義すること
- 実行結果のブレを防ぐための出力フォーマットを指定すること
- コピペしてすぐに使える完成版のプロンプトとして出力すること

---------
# History
ゼロベースで
下の厳格な要件・制約をすべて満たす、ハルシネーションのない回答せよ

# Target Tech Stack & Environment
- Runtime / Lang: Windows 10/11, .NET 9, C# 13, Visual Studio 2026, WPF (商用開発)
- Architecture: Feature-Based 構成, Microsoft.Extensions.Hosting (`CreateApplicationBuilder`), `Program.cs` の `Program` クラス内 `Main` 関数
- CLI Parsing: `Spectre.Console.Cli` (`TypeRegistrar` の実装必須, `Command` 派生 `RunCommand`)
- DI & Data / IO: `ITdmsService` / `TdmsService` 登録, NI公式 `nilibdcc.dll` (大容量TDMS読込), SkiaSharp, Dapper, SqlKata, SqlKata.Execution, DuckDB.NET.Data.Full, Npgsql, Parquet.Net 6 (`using Parquet;`, `using Parquet.Schema;`)
- MVVM & Reactive: `CommunityToolkit.Mvvm`, `R3` (Cysharp), `R3Extensions.WPF`, `Microsoft.Xaml.Behaviors.Wpf` (※UniRxの混入は絶対禁止)
- UI Components:
  - AvalonDock: `Dirkster99/AvalonDock` (NuGet: `AvalonDock`), XML名前空間は必ず `xmlns:xcad="https://github.com/Dirkster99/AvalonDock"` を使用 (`http://schemas.xceed.com/wpf/xaml/avalondock` は使用禁止)
  - ScottPlot 5: `ScottPlot.WPF.WpfPlot` (ScottPlot 4のAPIは絶対に使用禁止)
  - Parquet.Net 6: `Parquet.Net` (Parquet.Net 4,5 のAPIは絶対に使用禁止)

# Anti-Hallucination & Verification Rules (絶対遵守)
1. **完全実在APIの強制**: 回答内のすべての型、プロパティ、メソッド、名前空間は、指定されたライブラリの公式最新リポジトリに実在するもののみを使用すること。推測によるコード生成は厳禁。
2. **自己検証プロセスの徹底**: コード出力前に、R3, ScottPlot 5, AvalonDock, Parquet.Net 6 の公式仕様・API定義に合致しているかを内部でステップバイステップで検証すること。
3. **代替案の義務**: 確証のないAPIや存在しない機能に直面した場合は、勝手に生成せず「代替案の提示」または「実装不可の明記」を行うこと。
4. **バージョン・APIの厳格分離**:
   - R3: `DisposableBag` や `R3.ReactiveCommand` を活用し、ライフサイクル終了時に適切に破棄すること。
   - ScottPlot 5: 推奨パターン (`Plot plot = new(); plot.Add...; wpfPlot.Plot = plot;`) を厳守すること。
   - AvalonDock: `DockingManager` をルートとし、`LayoutRoot -> LayoutPanel -> LayoutDocumentPaneGroup` 等の公式階層構造を崩さないこと。
   - Parquet.Net 6: `using Parquet.Data;` や `ParquetFactor` などの旧API・非推奨クラスは絶対に使用禁止。

# Coding Standards & Modern C# 13 Features
- プライマリコンストラクタ、コレクション式、パターンマッチング、`IAsyncEnumerable`、`await using` などのモダンな C# 13 記法を積極的に活用すること。
- メモリ割り当ての最適化、スレッドセーフティ、堅牢な例外処理（`try-catch` / ログ出力配慮）を組み込むこと。
- `\WPFUI\Core` や各 Feature フォルダ等、下記のフォルダ構成に沿った適切な名前空間とクリーンな責務分離を行うこと。

\WPFUI
│  Program.cs
├─Core
│  ├─Abstractions
│  │      ITdmsService.cs
│  ├─Base
│  │      BoolToVisibilityConverter.cs
│  │      INavigationService.cs
│  │      NavigationExtensions.cs
│  │      NavigationService.cs
│  │      ResourceKeyToGeometryConverter.cs
│  │      ViewBase.cs
│  │      ViewModelBase.cs
│  ├─Collections
│  │      LruCache.cs
│  ├─Domain
│  │   └─Models
│  │          TdmsMetadata.cs
│  └─Themes
│          Converters.xaml
│          Icons.xaml
│          Styles.xaml
├─Features
│  ├─Shell
│  │      MainViewModel.cs
│  │      MainWindow.xaml
│  │      MainWindow.xaml.cs
│  ├─Measure
│  │   │  MeasureView.xaml
│  │   │  MeasureView.xaml.cs
│  │   │  MeasureViewModel.cs
│  │   │  MeasureService.cs
│  │   ├─Assets
│  │   │      Icons.xaml
│  │   │      Styles.xaml
│  │   └─Explorer
│  │           MeasureExpView.xaml
│  │           MeasureExpView.xaml.cs
│  │           MeasureExpViewModel.cs
│  │           MeasureExpService.cs
│  └─Waveform
│      │  WaveformView.xaml
│      │  WaveformView.xaml.cs
│      │  WaveformViewModel.cs
│      │  WaveformService.cs
│      ├─Assets
│      │      Icons.xaml
│      │      Styles.xaml
│      └─Explorer
│              WaveformExpView.xaml
│              WaveformExpView.xaml.cs
│              WaveformExpViewModel.cs
│              WaveformExpService.cs
└─Infrastructure
   ├─Cli
   │      RunCommand.cs
   │      TypeRegistrar.cs
   ├─Database
   │      DatabaseExtensions.cs
   │      DatabaseOptions.cs
   │      DbConnectionFactory.cs
   └─Persistence
       ├─Parquet
       │      Models.cs
       │      ParquetStorageService.cs
       └─Tdms
           │  TdmsService.cs
           │  TdmsWrapper.cs
           └─Native
                   TdmHandle.cs
                   TdmNative.cs

[質問/依頼]
Role: 
あなたは .NET 9 および C# 13, UX/UI,DB のエキスパートであり、Parquet.Net 6 の内部構造および最新API仕様に完全に精通した妥協を許さない最高峰のシニアソフトウェアエンジニアです。

Constraints:
- [Fact-Based] 架空のライブラリ、非推奨のAPI、存在しない仕様の捏造を厳格に禁ずる。
- [Traceability] 全ての技術的提案・コードに対し、実在する根拠または論理的裏付けを明示すること。
- [No-Guessing] 要件が不明確な場合、勝手な推論で補完せず、必ずユーザーに「設計を確定させるための質問」を要求すること。
Action:
上記の制約を完璧に守り、最高品質のUI/UX体験と、堅牢なDB/システム設計を統合した以下の要件を満たせ。

【要件】
あるシステム画面の構成やユーザ層について検討する。
- 画面は4ペインで構成され 左端はNavigatorで WorkSpace(中, 右上, 右下の画面)の切り替える
  Navigatorには計測タブ・履歴タブが並ぶ
- 右画面は上下に分割されており、右下の画面はFloating可能なDockingViewとなっており切り離せる (タブ種別ごとに別View)
- 中画面はExplorerの役割を持つ(Expand/collapse)
- 実装済みの計測タブにおいて
  - Explorer画面(中画面)にテスト条件やテスト開始ボタンが配置されている
  - テスト開始で複数のテスト項目が実施される
  - 右上画面にはテスト項目ごとの計測結果が並ぶ
  - 右下画面(Floating可能なDockingView)には計測時のテスト項目ごとのグラフがテスト完了時に表示される
- テスト結果やグラフデータは DBに保持されている

上記を踏まえ、新たに履歴/統計 画面を構築したい
- テスト実施ごとの履歴項目が並ぶ
- テストの履歴から選択し 計測結果やグラフの表示を行う
- テスト実行中も計測タブ画面から履歴画面に切り替え計測結果やグラフを表示し、計測実行中の結果と並べて確認したい
- 複数の過去の履歴から計測結果やグラフを並べて確認したい
- 履歴画面ではテストの履歴を絞り込み絞り込んだ情報でのいろいろな統計結果を表示したい

UI/UXの観点からより良い導線となる履歴/統計画面構成を構築する
案:
- 履歴/統計画面のExplorer画面(中画面)に実施履歴の一覧を表示し 絞り込み条件部を配置
  絞り込み条件の指定で一覧から選択可能にする
- 履歴/統計画面の右上画面には統計結果を表示 (統計方法切り替え等も)
- 右下画面(Floating可能なDockingView)には計測画面の情報と比較したい項目を配置する
  - Explorerの実施履歴の一覧 から 履歴指定を指定(D&D)すると 該当する履歴の 計測結果が表示 (複数選択可)
  - 計測結果 部を 計測画面の右下画面(Floating可能なDockingView)にD&Dすると該当するグラフが表示される

上記について、整理し要点を纏めよ
モダン且つ簡潔で最強に洗練されたより良い方法として 矛盾がないかを検証せよ。
検証結果を踏まえ、モダン且つ簡潔で最強に洗練されたより良いアーキテクチャを利点・欠点・評価とともに示せ
複数パターン(4パターン以上)検証し、最も洗練されたものを示せ

上記の結果を踏まえ、Historyの画面を再構築する (現状の実装はスケルトンのため無視/削除してよい)
基本となる画面構成やExplorer部の実装を仮のデータとともに示せ

