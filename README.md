# Inspect View
Inspect View is program, that can be used for creating machine vision system, for both hobby use and in small production lines. It is based on color detection to find if inspected component passes test.

<br/>

## Usage
### Minimal setup
Other than installed program, you need only USB camera. In this case you can only do inspections by hand and they will stop being passed if lighting in room changes. Also quality of camera matters a lot if you want to make inspection on small components or when color contrast between component and background is too small.

<br/>

### Best setup
For best results and possibility of automation, you need:
- good quality USB camera
- source of bright, white light
- background, that has high contrast with inspected components
- serial port device, that can communicate with Inspect View (such as microcontroller)

For automation on production line you need every inspected component to be in exact same position as previous ones. Also light source need to be in exact same place, directed is such way, that there is minimal amount of shadows on inspected component.
With such setup, colors for every inspections should be same if you use same component. It matters a lot, because Inspect View is based on color detection - it is basically checking if colors of inspected camera image are in range that user set.

Serial port device is needed for communication between production line and program. It is used to send signal to program for doing inspection, when component is ready and recieve signal from program, if inspection is passed or not. It can be something as simple as Arduino Uno.

<br/>

### How to use Inspect View
Inspect View can be used both for manual and automated inspections. In both cases preparing project for use is the same.

How to prepare project for inspection:
1. Connect to camera by clicking "Connection" > "Connect Camera" on menu bar<br/>
&emsp;1a. Window with camera list should open. To search for camera click "Search" button, select camera on list and click "Connect"<br/>
&emsp;1b. If you have more cameras connected, or searching didn't found your camera, change "Search for" field to bigger value
2. On middle of screen you have three windows called "Camera View", "Masked View" and "Limiter Masked View". After connecting to camera, image from it should show up on "Camera View". If not, there is something wrong with connection or camera (maybe you connected to wrong one?)
3. On toolbar you have 5 buttons and image that should say "OK" on green field for now. In order they are:<br/>
&emsp;3a. Take Photo - it takes currect image from camera and displays it on "Camera View"<br/>
&emsp;3b. Ellipse Tool - creates ellipse limiter<br/>
&emsp;3c. Rectangle Tool - creates rectangle limiter<br/>
&emsp;3d. Test Button - does inspection on image displayed on "Camera View"<br/>
&emsp;3e. NOK/OK Indicator - tells user if inspection passed or not<br/>
&emsp;4f. Start Button - starts automated inspection if connected to serial device
4. With camera connected prepare your workspace, by setting angles for both camera and light. Click "Take Photo" every time you make changes, to view if everything is centered and well lit
5. It is time to create first limiter. Limiter is area, that will be inspected. You can have unlimited amount of limiters in one project and every one of them will do one inspection, based on color data you give it. They can be both ellipse and rectangle and are created by selecting "Ellipse Tool" or "Rectangle Tool" and clicking on "Camera View"
6. To move limiter select it by clicking on it and then drag and drop it. To remove it use "Delete" key on keyboard. To resize it use scroll wheel while selected. If you hold "Left CTRL" or "Left SHIFT" while resizing, you will only resize its width or height
7. On left side you can see all data of selected limiter. You can change its name and color range, that limiter will inspect for. By clicking "Get color data", program will automatically do most of the job for you, by taking color data from inside of limiter, based on standard deviation of color values
8. After changing color data there should be black and white image on "Masked View". Every white pixel corresponds to pixel on "Camera View" that is in range of limiter's color data
9. Change red, green and blue min/max values, by dragging sliders in limiter data panel. By using trail and error, change them, so part of component, that you are inspecting, is as white as possible on "Masked View", while everything else stays black 
10. On "Limiter Masked View" you can see only pixels that are both in range of color data and inside selected limiter. Near it's name there is pixel count. It's amount of white pixels that are inside limiter
11. Copy pixel count from "Limiter Masked View" to "Pixel count" field on limiter's data panel
12. Click "Test Button" to do inspection on selected limiter. If "NOK/OK Indicator" says "OK" it means that component passed inspection for selected limiter. If it says "NOK" it means that it didn't pass. Make sure you changed "Pixel count" value (see point 11) or change "Count delta"
13. "Count delta" value tells how much "Pixel count" value can deviate from color data for inspection to be still passed. It needs to be set in such way, that all extreme cases still pass inspection
14. Now change component to case that should not pass inspection. Click "Test Button" and if "NOK/OK Indicator" says "NOK", it means that color data are good for now. If it says "OK" change "Pixel count" and "Pixel delta" until it says "NOK" after doing inspection. There might be also need to change color sliders
15. Test all extreme cases for component. Set all color data until all of them pass inspection
16. Test again as in point 14
17. Do points 5-16 until you have limiters for all parts of component you want to inspect

<br/>

Now you have all limiters ready. You can rename them if needed and you can save your project to *.xml file in "File" menu. To do manual test you just need to prepare component and test limiters by selecting them and clicking "Test Button". For automated inspections you need to:
1. Connect to serial device by clicking "Connection" > "Serial Port" > "Connect Serial Port". It will open similar window to camera connection. Search for serial device and connect
2. Send test signal by clicking "Connection" > "Serial Port" > "Test Connection"
3. If connection is ok and serial device is programmed correctly, you should get "HANDSHAKE_OK" response dialog
4. Click "Start Button". It will now work in automated mode

<br/>

### Program <=> Serial device communication
Communication is done be sending simple messages in form of string of text. They are:
1. Sent by Inspect View to serial device<br/>
"IV_HANDSHAKE" - Send handshake - used to test connection between Inspect View and serial device<br/>
"IV_INSPECT_OK" - Inspection successfull<br/>
"IV_INSPECT_NOK" - Inspection unsuccessful<br/>

3. Sent by serial device to Inspect View<br/>
"IV_HANDSHAKE_OK" - Respond to Inspect View handshake<br/>
"IV_DO_INSPECT" - Send signal to do inspection<br/>

For correct comunication you need to program your serial device, so it supports those messanges and responds depending if inspection passed or not. In case of passed inspection it might move production line and if inspection did not pass, it could stop the line and send alarm signal for operator.
