# This is a Markleft document

It is a [Markdown](https://daringfireball.net/projects/markdown/syntax)-like renderer with some useful features for use within the Unity Editor 😉

## Feature list

* Headers of multiple sizes, paragraphs, **bold**, *italic* and `monospace`
* Bullet-point lists
  * Starting with `*`
  - Starting with `-`
* Task lists (non-interactive at the moment)
  - [ ] unchecked
  - [X] checked!
* Links
  - [To external websites (using your browser)](https://unity.com/)
  - [To assets relative to the current asset location](TutorialInfo/Icons/URP.png)
  - [To assets with absolute address locations](Assets/Scenes/SampleScene.unity)
  - [To menu entries](menu:Window/Package Management/Package Manager)
  - [To project settings](project:Project/Quality)
  - [To user preferences](prefs:Preferences/External Tools)
  - [That trigger scripts](static:ReadmeEditor.ExampleCall)
* Images
  * ![like this one](TutorialInfo/Icons/URP.png)
* Code blocks (no syntax highlighting yet)
  ```
  void Hello(string world) {
    Debug.Log($"Hello {world}");
  }
  ```
* Horizontal splits
-----

[Remove the Sample assets](static:ReadmeEditor.RemoveTutorial)