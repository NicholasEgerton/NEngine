# NEngine
Verlet simulation using compute shaders in Unity. (In progress)

<img width="426" height="240" alt="Example" src="https://github.com/user-attachments/assets/2de894fb-763d-4049-9d9d-de87c111b49b" />

I initially started this project in 2024, and left it for a couple years. The legacy version aimed to draw particles directly to the screen in the compute shader.

In 2026 I revisited this project to completely remake it from scratch. To get the basics right first, this initial version uses compute shaders for calculations, but still uses the CPU and Unity GameObjects for rendering. It also uses inefficient O(n^2) algorithms.

I intend to work on optimising the underlying algorithms first, before finally converting all processing and rendering to use compute shaders.

Made by Nicholas Egerton.
