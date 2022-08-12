# Slackord 2 ![Slackord 2 Logo](https://i.imgur.com/PyVjqzL.png)

Slackord 2 is a cross-platform application that parses JSON chat history file exports from Slack and imports them into Discord instantly.
Slackord 2 is the .NET6.0 version of the [Slackord 1.0](https://github.com/thomasloupe/Slackord) Python/Tkinter app with significant feature additions.

# Demo
![Slackord 2 Demo](https://i.imgur.com/iI9JHRj.gif)

# Features
1. Cross platform. Works on Windows(x86/x64), Mac(x64), and Linux(x64).
1. Post messages, images, and attachments to any Discord channel or Direct Message conversation.
1. Debug window. Unlike Slackord 0.5 and 1.0, Slackord 2 lets you know exactly what's happening in real-time. You'll know what messages are being posted, how they'll be formatted, and what the bot is doing before sending messages to their destinations.
1. Automatic rate-limiting. Slackord 2 limits the messages it sends over time so it doesn't spam your target.
1. Remembers your bot's token.
1. Privacy first. Slackord 2 checks if user messages have a display name and will attempt to keep real names private unless there isn't one.
1. Data safety. Your data is yours. Slackord 2 works completely off your local connection and machine. It doesn't connect to anything but the server and channels you tell it to.
1. Update checks. Slackord 2 has the ability to check for updates in the "Help" context menu. Get the latest version with new features easily!

# Getting Started
1. Download the [latest](https://github.com/thomasloupe/Slackord2/releases) Slackord 2 release and extract the contents.
1. Create a Discord bot [here](https://discord.com/developers/applications). Under OAuth2>URL Generator, set the bot's "SCOPES" to "bot". Underneath, in "BOT PERMISSIONS", set the bot's permissions to either "Administrator" or "Send Messages". Copy the "GENERATED URL" link below the "BOT PERMISSIONS", and paste into a browser. Join the bot into your desired server. Visit the "Bot" link below OAuth2, generate and copy the bot's private token.
1. Visit the [Discord Developer Portal](https://discord.com/developers/applications), naviagate to the "Bot" page underneath "OAuth2". Upload an image for your bot if desired, click "Regenerate Token", select "Yes, do it!", then click the "Copy" button to the left of "Regenerate".
1. Run Slackord.
2. Select Settings>Enter Bot Token and paste the copied token into the text field. Slackord will remember your token if you close it.
3. Select File>Import JSON and select a Slack JSON chat history file to import.
4. Select Settings>Bot Connection>Connect to connect the bot to your server.
5. Visit the Discord channel or DM you wish to import Slack chat history to and type **!slackord** (case insensitive).
6. Messages will begin posting.

# Important: Please Read!
1. If you need help, please feel free to get in touch with me on [Twitter](https://twitter.com/acid_rain), or open a new issue if it doesn't already exist.
1. Slackord and Slackord 2 are free, and they will always be free. Please do not pay anyone for this application. However, if you found Slackord or Slackord 2 worth donating something, you can donate from within Slackord 2 by accessing "Donate" from the "Help" context menu or just [click here](https://paypal.me/thomasloupe).
1. I have decided against importing multiple JSON files to disincentivise using Slackord maliciously. You can, however, merge multiple JSON files into one with [this tool](https://tools.knowledgewalls.com/onlinejsonmerger)  or many others that exist online which will achieve the same result as importing multiple JSON files.
1. Slackord is something I created in my spare time to continue learning multiple programming languages. I would not consider myself an "expert" programmer, and should probably not be treated as such.
