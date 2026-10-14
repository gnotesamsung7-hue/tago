# Mark Dave's TAGO

A side-scrolling stealth game set in modern Tondo, Manila. You owe Bossing Tiburon ₱48,000 and tonight he's shouting at your gate. Sneak through the eskinita, cross Juan Luna, and reach the bus terminal to escape to the province.

Built with Unity 2022.3, entirely in code: no scenes, images, or prefabs to edit. Everything (street, houses, people, traffic, HUD) is created at runtime by `Assets/Scripts`. The APK is built by GitHub Actions.

## How it plays
- **Move** with the left side of the screen. Up/down changes depth: front sidewalk, road, back sidewalk.
- **Tago** crouches. Crouch right behind a drum, crate, stall, or parked tricycle to hide.
- **Silip** (hold, while hidden) peeks over the cover. Goons are hard to see while you're hiding, so peek to check where they are. Peeking makes you a little visible.
- **Blend in**: walk inside a crowd (jeepney stop, tambays, passengers) and goons barely notice you. Running breaks it.
- **Pasok**: hide inside a house. The owner kicks you out after a few seconds, and if a goon saw you go in, he'll search the house.
- **Takbo** (hold) runs. Fast but loud.
- **Tansan** throws a bottle cap to make goons check the noise.
- **Traffic**: jeepneys and tricycles knock you back with a loud honk. A big vehicle passing between you and a goon blocks his view.
- **Getting caught**: the goons take half your cash and add 20% interest, and you retry the area.

## Project layout
- `Assets/Scripts/TagoGame.cs` — gameplay, opening scene, goon AI, traffic, HUD, touch controls
- `Assets/Scripts/Stages.cs` — the three areas (eskinita, Juan Luna, bus terminal): cover, goons, crowds, items
- `Assets/Scripts/Art.cs` — all sprites, painted pixel by pixel in code
- `Assets/Editor/BuildScript.cs` — creates the scene and builds the APK
- `.github/workflows/build-apk.yml` — the GitHub Actions build

## One-time setup
1. Create a GitHub repo (e.g. `tago`) and upload everything in this folder, including the hidden `.github` folder.
2. Create a free Unity account at unity.com. Turn off two-factor sign-in, or the build can't log in.
3. Get a Unity license file (`.ulf`). This needs Unity Hub on *any* computer, once (a friend's PC or a computer shop is fine):
   - Install Unity Hub, sign in, go to Preferences → Licenses → Add → Get a free personal license.
   - Open `C:\ProgramData\Unity\Unity_lic.ulf` in Notepad and copy everything.
4. In the repo: Settings → Secrets and variables → Actions → New repository secret. Add:
   - `UNITY_LICENSE` — the full contents of the `.ulf` file
   - `UNITY_EMAIL` — your Unity account email
   - `UNITY_PASSWORD` — your Unity account password
5. Actions tab → Build TAGO APK → Run workflow. The first build takes about 20–30 minutes.
6. Download `Tago-APK` from the run's Artifacts section, unzip it, and install `Tago.apk` on your phone.

## If the build fails
- **Version / image not found:** change the version in `ProjectSettings/ProjectVersion.txt` to any 2022.3.x listed at game.ci/docs/docker/versions.
- **License errors:** re-check the three secrets. The `.ulf` contents must be pasted whole, starting with `<?xml`.
- Anything else: copy the red error lines from the Actions log and send them over.
