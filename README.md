# RoCoCo

## 実行
- クラウドにあるサーバと通信してシミュレーションを行います。ネットに接続した状態でBuildディレクトリ内のRoCoCo.exeを起動してください。
- 初期状態のロボット２台のシミュレーションは、ジョイスティックなしでも実行可能です。

※実行にはユーザIDの発行が必要です。

####  (図1) RoCoCo起動時の管理者画面
![RoCoCo-small](https://user-images.githubusercontent.com/75976760/102228401-00d5f700-3f2e-11eb-923d-f3ca38ceebc4.png)

左側がエージェント１用、右側がエージェント２用の画面。PCに拡張ディスプレイを２台接続した場合、１台目のディスプレイに左側、２台目のディスプレイに右側の画面が表示されます。

画面上の各オブジェクトは以下の通り。

| オブジェクト | 色 |
| ------------- | ------------- |
| エージェント１ | 赤 |
| エージェント２ | 青 |
| 机 | 緑 |
| 障害物 | 黒 |
| ゴール地点 | 薄灰 |

#### (図2) 開発に使用したジョイスティック「[Logicool G ゲームパッド F310r](https://www.amazon.co.jp/gp/product/B00CDG799E/)」
![F310-small](https://user-images.githubusercontent.com/75976760/102229342-05e77600-3f2f-11eb-8557-81bb78a72465.png)

### 開始
- ジョイスティックの **B** ボタンまたはPCの **SPACE** キー押下により、シミュレーションを開始します。

### 操作
- 設定ファイルの"player_num"によって操作方法が異なります。

| "player_num" | 操作方法 |
| ------------- | ------------- |
| 0  | ２つのエージェント（ロボット２台）が自動で動いてゴールまで机を運びます。|
| 1  | 赤いエージェント（人間）をジョイスティックの **左スティック** で操作し、青いエージェント（ロボット）と協調してゴールまで机を運びます。|
| 2  | 赤いエージェント（人間１人目）と青いエージェント（人間２人目）を２台のジョイスティックの **左スティック** で操作してゴールまで机を運びます。|

### 終了
- 机がゴール地点に近づくと自動ゴール判定により、背景色が黒に変わりシミュレーションが終了します。
- シミュレーションが終わらない場合や途中で終了したい場合、ジョイスティックの **X** ボタンまたはPCの **ESC** キー押下により、シミュレーションを終了します。

## 設定
- Build/RoCoCo_Data ディレクトリ内の設定ファイル「settings.json」を編集します。

（設定例）
```
{
  "host": "XXX.XXX.XXX.XXX",  #サーバのアドレス
  "port": 80,                 #サーバのポート番号
  ...
  "player_num": 0,   #プレイヤーの数（0: ロボット２台、1: 人間とロボット、2: 人間２人）
  "agent1_task_dynamics_weight": 0.5,   #エージェント１の制御時重み
  "agent2_task_dynamics_weight": 0.5,   #エージェント２の制御時重み
  ...
}
```

## ログ
- RoCoCoはシミュレーションごとにログディレクトリ「log_yyyymmdd_hhmmss」を作成し、ログを保管しています。
- ログディレクトリ内のファイル「posvel.csv」には、エージェントの位置と速度が時系列で記録されています。posvel.csvの形式はカンマ区切りで一行に13要素。位置と速度は全て２次元（x, y）で以下の順に出力されます。
```
時刻,エージェント1の位置,実速度,加えた速度,エージェント2の位置,実速度,加えた速度
```
（例）
```
2020/11/08 23:05:43.071,2.501,1.105,-0.001,-0.003,0.000,0.000,2.629,1.895,-0.043,0.003,-0.044,0.000
```

## 再生モード
- 設定ファイルの"replay"をtrueとして起動すると再生モードになります。
- 設定ファイルの"log"に再生対象のログディレクトリを指定します。"log_\*"としておくと最新日時のシミュレーションが再生されます。
- 起動後自動で再生されますが、再び最初から再生するにはジョイスティックの **B** ボタンまたはPCの **SPACE** キーを押下します。

