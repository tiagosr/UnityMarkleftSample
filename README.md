# Markleft Renderer for Unity Editor

Markleft is a [Markdown](https://daringfireball.net/projects/markdown/syntax)-like renderer with some useful features for use within the Unity Editor.

## Feature list

* Headers of multiple sizes, paragraphs, **bold**, *italic* and `monospace`
* Bullet-point lists
  * Starting with `*`
  - Starting with `-`
* Links
  - To external websites (using your browser): `[External link](https://unity.com/)`
  - To assets relative to the current asset location: `[Relative asset path](TutorialInfo/Icons/URP.png)`
  - To assets with absolute address locations: `[Absolute asset path](Assets/Scenes/SampleScene.unity)`
  - To menu entries: `[To Package Manager](menu:Window/Package Management/Package Manager)`
  - To project settings: `[To Quality Settings](project:Project/Quality)`
  - To user preferences: `[To External Tools](prefs:Preferences/External Tools)`
* Images
  * `![as internal references](TutorialInfo/Icons/URP.png)`
* Code blocks (no syntax highlighting yet)
  ```
  void Hello(string world) {
    Debug.Log($"Hello {world}");
  }
  ```
* Horizontal splits

## Usage guide

Add the `esque.ma.markdown-editor` package to your local project packages, and in your `OnInspectorGUI()` or similar call use:
```
    MarkleftEditor.MarkleftRenderer.Draw(<markleftString>,<currentAssetPath>);
```
where `markleftString` is the markleft code you want to draw, and `currentAssetPath` is the `AssetDatabase` path of the asset to reference any relative links from.


-----

## License
### The MIT License (MIT)

Copyright © 2026 Tiago Rezende

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.