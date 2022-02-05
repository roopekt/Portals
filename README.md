# Portals 

A "game" made with unity featuring impossible geometry. This is done with seamless see-through portals.
<br/><br/>
![Portal example](https://github.com/roopekt/Portals/blob/main/ReadmeData/portal_example.png)

## Installation 

__IMPORTANT__: This project uses multiple resources (skybox, textures) that can not be redistributed as part of an open source project.
These files will not be included when you clone the project.

 1. Clone the repository
	```shell
	git clone https://github.com/roopekt/Portals.git
	```
2. Install Visual Studio
	https://visualstudio.microsoft.com/downloads/
3. Install Unity
	- Install Unity Hub from https://unity3d.com/get-unity/download
	- Using the hub, install  an editor with the version 2019.4.4f1 (LTS). Remember to check visual studio integration when prompted about additional packages.
4. Locate and open the project trough Unity Hub
5. Install ProBuilder 4.2.3 through the package manager
6. Install ProGrids 3.0.3 through the package manager

## Usage 

 - Turn camera with mouse
 - Move with WASD keys
 - jump with space
 - close by pressing esc

## How it works 

1. If there are objects touching the portals, there will be a clone of it on the other side, so that it will be properly rendered.
2. The player camera finds all portals that are visible to it.
3. Each portal finds all portals visible to it, until there are no more visible portals or resources to render them.
4. A special projection matrix is calculated for each portal camera, so that its near plane perfectly matches the portal quad.
<br/><br/>
![Portal example](https://github.com/roopekt/Portals/blob/main/ReadmeData/frustum_diagram.png)
5. A camera renders all the portals to their textures. This is done using the just calculated projection matrices, and in backwards order to how they were found.
6. The player camera renders.
7. Objects that have moved through portals are teleported.

## License 

This project is distributed under the MIT License. See `LICENSE.txt` for more information.

## Acknowledgments 

- The skybox is part of [Free HDR Sky](https://assetstore.unity.com/packages/2d/textures-materials/sky/free-hdr-sky-61217) by ProAssets (free asset)
- Textures have been downloaded from [Poliigon](https://www.poliigon.com/) (all free textures)
- This project has a screen space shader that is based on a [tutorial](https://www.youtube.com/watch?v=cuQao3hEKfs) by [Brackeys](https://www.youtube.com/channel/UCYbK_tjZ2OrIZFBvU6CCMiA)

## Caveats

- There are some visual artifacts when close to portals
	- low resolution
	- near plane of the player camera clips through the portal 
<br/><br/>
![bad visual artifacts](https://github.com/roopekt/Portals/blob/main/ReadmeData/artifacts.png)
<br/><br/>
- Objects passing through portals have only one collider, so they can act weird when trying to push them from the wrong side of the portal
