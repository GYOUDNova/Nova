# Nova – Hand Gesture Recognition for Unity <img src="https://media.githubusercontent.com/media/GYOUDNova/Nova/refs/heads/main/Assets/Images/NOVA_Logo.png" alt="Nova Logo" width="75" style="float: right; margin-left: 10px;"/>

[![Format Check](https://github.com/GYOUDNova/Nova/actions/workflows/formatCheck.yml/badge.svg)](https://github.com/GYOUDNova/Nova/actions/workflows/formatCheck.yml)
[![Test Runner 😎](https://github.com/GYOUDNova/Nova/actions/workflows/testRunner.yml/badge.svg)](https://github.com/GYOUDNova/Nova/actions/workflows/testRunner.yml)

> A drop‑in, all‑in‑one gesture input system for Unity. Nova wraps hand‑landmark detection (via MediaPipe) and a flexible gesture recognizer into Unity‑friendly components, prefabs, and events. This way developers can focus in the important aspect of their application, not troubleshooting multiple libraries and their compatibilities.

---

## What is Nova?

Nova is a Unity package that turns a webcam (or video feed) into high‑level **gesture events**. Under the hood we use **MediaPipe** to generate hand landmarks, then provide:

* **Ready‑to‑use prefabs** for camera/processing/visualization
* **Configurable Gestures** and **Gesture Chains** you can author in the Editor
* **Unity Events** you can hook into UI, input systems, or scripts
* **Sample scenes** that demonstrate common patterns (menu navigation, character control, racing)

Nova’s goal is to be **package‑level plug‑and‑play**, import it and build.

## Why Nova (vs. just MediaPipe/OpenCV)?

* **All‑in‑one Unity package.** No extra glue code or native setup.
* **Editor‑first workflow.** Create gestures and gesture chains as assets; tweak settings live.
* **Drop‑in prefabs.** Get to a working prototype in minutes.
* **Battle‑tested samples.** Three scenes show practical interaction patterns.

> Looking for installation, platform support, tutorials, or troubleshooting? **All of that lives in the Wiki.**
> 👉 **Read the Wiki:** [https://github.com/GYOUDNova/Nova/wiki](https://github.com/GYOUDNova/Nova/wiki) (or use the repo’s *Wiki* tab)

---

## Quick look

**Main Screen**
</br>
<img width="500" height="500" alt="Nova Main Menu" src="https://github.com/user-attachments/assets/cada7677-eaac-49c5-a7eb-d121af5508f0" />


**In‑scene gesture control**
</br>
<img width="800" height="1000" alt="image" src="https://github.com/user-attachments/assets/8174c4eb-e696-451b-aa62-cc332ee09d4f" />

---

## Samples (Assets/Samples\~)

All samples are included under `Assets/Samples~`:

* **SampleMenu** – A kiosk‑style UI you fully navigate with hand gestures.
  
  <img width="725" height="725" alt="Sample Menu" src="https://github.com/user-attachments/assets/ae8b2457-2b27-4b0a-9050-e58fd4922c6e" />

* **Rollaball** – Classic Unity “roll a ball” tutorial reimagined with gesture input.

  <img width="725" height="725" alt="Rollaball" src="https://github.com/user-attachments/assets/7e10b350-7052-4938-8ce6-17cb1828e25a" />

* **Unity Kart** – Kart racing (Mario‑kart‑style) with gestures.

  <img width="725" height="725" alt="image" src="https://github.com/user-attachments/assets/366fff0d-0a3b-44fd-93e9-192852734486" />
  
---

## Getting started

* **Install Nova** → see the Wiki’s *Installation* page.
* **Create a Gesture** or a **Gesture Chain** → see the Wiki’s authoring guides.
* **Troubleshooting** (camera, performance, platforms) → see the Wiki’s FAQ.

📘 **Wiki:** [https://github.com/GYOUDNova/Nova/wiki](https://github.com/GYOUDNova/Nova/wiki)

---

## Authors & Credits

**Authors**

* Hayden Auterhoff
* Omar Nunez
* Ryan Samii
* Talon Ernst

**Credits**

* Built on the [**MediaPipe Unity Plugin**](https://github.com/homuler/MediaPipeUnityPlugin) for hand‑landmark detection.
* Thanks to the Unity community samples that inspired our scenes.

---

## License

See **LICENSE** in this repository.
