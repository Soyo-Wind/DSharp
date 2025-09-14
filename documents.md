# D# ドキュメント

## 🧾 概要

**D#** は、インデントベースのシンプルな構文で記述されたスクリプトを C++ に変換するトランスパイラ用 DSL（ドメイン固有言語）です。

- インデントによるブロック制御
    
- Python風の書き方で C++ に変換
    
- 学習・教育目的や簡易スクリプト作成に便利
    

---

## 🚀 使用方法

<t|f>のところは、ログ出力です.
### windows

`./dsharp.exe <スクリプトファイル> <t|f>`
### linux

`./dsharp <スクリプトファイル> <t|f>``

出力ファイル：`main.cpp`

---

## 🛠 文法仕様

### 変数定義

`def <型> <変数名> <初期値>`

### 変数代入

`q <変数名> <新しい値>`

---

### リスト操作

`add <リスト名> <値>`
`del <リスト名> <インデックス>`
`ins <リスト名> <インデックス> <値>`

---

### 出力

`out <値>       # 改行なし`
`outln <値>     # 改行あり`

例：

`outln "Hello"`

---

### 入力

`in <変数名>`

---

### 条件分岐

```
if <条件>
    ...
elif <条件>
    ...
el
    ...
```


---

### 繰り返し

`while <条件>`
`for <変数(teg)> <開始値> <条件> <増加値>`
`each <変数> <リスト>`

---

### 関数定義

```
func <戻り値の型> <関数名> <型1> <引数1> <型2> <引数2> 
	...
    ...
    return <値>
```


例：

```
func teg add teg a teg b
     return (a + b)
```

---

### 関数呼び出し

`call <関数名>(<引数>, ...)`

---

### ヘッダ読み込み

`use <ヘッダ名>  # 例: use vector`

---

### 特殊構文

`break`
`continue`
`quit <終了コード>`

---

## 🔤 対応型一覧

|DSL名|C++型|
|---|---|
|teg|int|
|cim|double|
|tnil|bool|
|rin|string|
|lis:X|vector<X>|
|lis:lis:X|vector<vector<X>>|
> [!TIP]
>lisは、`lis:rin`のように使います.
>`lis:lis:rin`→`vector<vector<string>>`のような使い方もできます.


---

## 📄 出力される C++ 構造

- 全コードは `int main()` 内、または関数に変換されます
    
- `QUITCODE` 変数で終了コードを制御できます
    
- 関数定義は先頭にまとめられ、main内に呼び出しが配置されます
    

---

## 🐞 ログ出力（デバッグ用）


出力内容（例）：

`Line 5: indent=1, currentIndent=1, funcIndent=0, mainBlockStack.Count=1, funcBlockStack.Count=0`

---

## 📝 エラー例

- `Unknown command`
    
- `Invalid <構文> syntax`
    
- `Unclosed parenthesis`
    
- `Function <name> already defined`
    

---

## 🗺 今後のTODO（例）

- 型チェックの厳密化
    
- より柔軟なエラーメッセージ
    
- import機能（外部ファイル読み込み）
    
- IDE統合／補完
    
- Web上での実行（REPL）
    

---

## 📚 付録：全構文一覧

|コマンド|説明|
|---|---|
|`def`|変数定義|
|`q`|変数代入|
|`add`|リストに追加|
|`del`|リストから削除|
|`ins`|リストに挿入|
|`out`/`outln`|出力|
|`in`|入力|
|`if`/`elif`/`else`/`el`|条件分岐|
|`for`/`while`/`each`|ループ|
|`func`|関数定義|
|`call`|関数呼び出し|
|`use`|C++ヘッダを読み込み|
|`return`|関数からの戻り値|
|`quit`|強制終了|
|`break`|ループ中断|
|`continue`|ループスキップ|
