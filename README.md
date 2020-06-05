# Slackord 2 ![Slackord 2 Logo](https://i.imgur.com/Rdbf4gI.png)

Slackord 2 is an application that parses JSON chat history file exports from Slack and imports them into Discord instantly.
Slackord 2 is the .NET version of the [Slackord 1.0](https://github.com/thomasloupe/Slackord) Python/Tkinter app with significant feature additions.

# Demo
![Slackord 2 Demo](https://i.imgur.com/iI9JHRj.gif)

# Features
1. Post messages to any Discord channel or Direct Message conversation
1. Debug window. Unlike Slackord 0.5 and 1.0, Slackord 2 lets you know exactly what's happening in real-time. You'll know what messages are being posted, how they'll be formatted, and what the bot is doing before sending messages to their destinations.
1. Automatic rate-limiting. Slackord 2 limits the messages it sends over time so it doesn't spam your target.
1. Remembers your bot's token.
1. Privacy first. Slackord 2 checks if user messages have a display name and will attempt to keep real names private unless there isn't one.
1. Data safety. Your data is yours. Slackord 2 works completely off your local connection and machine. It doesn't connect to anything but the server and channels you tell it to.
1. Update checks. Slackord 2 has the ability to check for updates in the "Help" context menu. Get the latest version with new features easily!

# Getting Started
1. Download the [latest](https://github.com/thomasloupe/Slackord2/releases) Slackord 2 release and extract the contents.
1. Create a Discord bot [here](https://discord.com/developers/applications), copy the bot's private token, and invite it to the desired server.
1. Run Slackord.
1. Select a Slack JSON chat history file to import.
1. Enter your bot's token.
1. Connect the bot to your server.
1. Visit the channel you wish to import Slack chat history to and type **!slackord**.

# Important: Please Read!
1. If you need help, please feel free to get in touch with me on [Twitter](https://twitter.com/acid_rain), or open a new issue if it doesn't already exist..
1. Slackord and Slackord 2 are free, and they will always be free. Please do not pay anyone for this application. However, if you found Slackord or Slackord 2 worth donating something, you can donate from within Slackord 2 by accessing "Donate" from the "Help" context menu or just [click here](https://paypal.me/thomasloupe).
1. At this time, I have decided *not* to include the ability to import multiple JSON files. This may or may not change in the future.
1. Slackord is something I created in my spare time to continue learning multiple programming languages. I would not consider myself an "expert" programmer, and should probably not be treated as such.
