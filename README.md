# PixelTracker
This is a small application running in the background on your PC tracking the movements of your cursor. Every pixel covered by your cursor is saved to the hard drive so you can eventuall see how much your cursor has traveled. I got the idea after browsing [this](https://www.reddit.com/r/Showerthoughts/comments/3ywil0/i_wonder_if_my_cursor_has_passed_over_every_pixel/?ref=share&ref_source=link) reddit thread, and couldn't stop thinking about it. 

## Usage
Compile the program with Visual Studio or your compiler of choice, and run the excecutable. It should start tracking your cursor across the screen and display the trails in red. There will be a little icon in your tray to toggle the overlay (by double-clicking) or exit the application (by right clicking).

You can also tweak a few parameters in the code. All variables are in GlobalSettings.cs. You can change `mainDelay` and `waitDelay` to adjust how often it will save the cursor data to the hard drive (higher delays will be nicer to your hard drive). You can also change `trackColor` to change the color of the overlay, and `updateInterval` to change how often the overlay refreshes with new trails. `countPixelHits` is a boolean to enable the heatmap-style. There is now a count for every pixel on every screen ke...(line truncated)...

Data will be saved in the working directory of the application, with one file for each screen. I haven't actually tested this with multiple screens as I have no extra screens, so I'm not sure if that works as expected. 