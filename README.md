# Twin Dash

A small split-lane 2D runner: control two runners, jump over obstacles, and see how long each one survives.

## Find this repository

The repository folder in the Arena workspace is `/home/user/game`. In a terminal, run:

```bash
cd /home/user/game
pwd
ls -la
```

`pwd` prints the current folder; `ls -la` lists its contents (including the hidden `.git` folder).

## Play the browser preview

Open `web/index.html` in a browser, or run a local server from the repository root:

```bash
python3 -m http.server 8000
```

Then open `http://localhost:8000/web/`.

Controls: **W** or **Space** jumps the upper runner; **↑** jumps the lower runner. Press **R** to restart.

## Open in Unity

Open the `unity/` folder with Unity Hub (Unity 2022.3 LTS or newer, 2D Core). Open `Assets/Scenes/TwinDash.unity` and press Play. The game builds its simple graphics and gameplay at runtime; no external packages are required.
