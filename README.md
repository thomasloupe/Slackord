# Slackord2 ![Slackord2 Logo](https://i.imgur.com/PyVjqzL.png)

Slackord2 is a cross-platform application that parses JSON chat history file exports from Slack and imports them into Discord instantly.
Slackord2 is the .NET6.0 version of the [Slackord 1.x](https://github.com/thomasloupe/Slackord) Python/Tkinter app with significant feature additions.

# Demo
![Slackord2 Demo](https://i.imgur.com/iI9JHRj.gif)

# Features
1. `Cross platform` - Works on Windows(x86/x64), Mac(x64), and Linux(x64).
1. `Post everything` - Post messages, images, and attachments to any Discord channel or Direct Message conversation.
1. `Robust Debug window` - Slackord2 lets you know exactly what's happening in real-time. You'll know what messages are being posted, how they'll be formatted, and what the bot is doing before sending messages to their destination channels.
1. `Automatic rate-limiting` - Slackord2 limits the messages it sends over time so it doesn't spam your target.
1. `Ease-of-use` - Slackord2 only needs to be set up once, and it'll remember your bot's token, making future imports easy.
1. `Privacy first` - Slackord2 checks if user messages have a display name and will attempt to keep real names private unless there isn't one.
1. `Data safety` - Your data is yours. Slackord2 works completely off your local connection and machine.
1. `Update checks` - Slackord2 has the ability to check for updates in the `Help` context menu. Get the latest version with new features easily!

# Getting Started
1. Download the [latest](https://github.com/thomasloupe/Slackord2/releases) Slackord2 release for your OS and extract the contents.
1. Create a Discord bot [here](https://discord.com/developers/applications) by selecting "New Application" at the top-right.
1. Name your bot "Slackord2", or any preferred custom name.
1. Under OAuth2>URL Generator, set the bot's "SCOPES" to "bot". This opens a new menu called "BOT PERMISSIONS" below.
1. In "BOT PERMISSIONS", set the bot's permissions to "Administrator". This allows Slackord2 to post to private channels, too.
1. Copy the "GENERATED URL" link below the "BOT PERMISSIONS", and paste into a browser.
1. Join the bot into your desired server.
1. In the "Bot" page underneath "OAuth2", Upload an image for your bot if desired.
1. Click "Reset Token", select "Yes, do it!", then click the "Copy" button to the left of "Regenerate". Keep this secret token handy.
1. Ensure that both "PUBLIC BOT" and "REQUIRES OAUTH2 CODE GRANT" sliders are turned off.
1. In "Privileged Gateway Intents", tick the slider to enable "MESSAGE CONTENT INTENT".
1. Follow the instructions below based on which Operating System you downloaded Slackord2 for.

# Running Slackord2 for Windows
1. Run Slackord2.
1. Select `Settings>Enter Bot Token` and paste the copied token into the text field. Slackord2 will remember your token if you close it.
1. Select `File>Import JSON` and select a Slack JSON chat history file to import.
1. Select `Settings>Bot Connection>Connect` to connect the bot to your server.
1. Visit the Discord channel or DM you wish to import Slack chat history to and type `!slackord` (case insensitive).
1. Messages will begin posting.

# Running Slackord2 for Mac/Linux
1. Copy your bot token into the `Token.txt` file in the root directory, or leave it blank, run Slackord2 and paste it into the CLI when prompted to do so. Slackord2 will remember your token if you close it.
1. Place any Slack JSON files you wish to parse inside of the `Files` directory.
1. Run Slackord2.
1. Slackord2 will ask you to pick from a numbered list of JSON files to parse. Enter the numerical value for the JSON file you wish to parse. Parsing will begin and the bot will connect to your Discord server.
1. Visit the Discord channel or DM you wish to import Slack chat history to and type `!slackord` (case insensitive).
1. Messages will begin posting.
1. Restart the Slackord2 app and repeat. (TODO: Don't require a restart to post a new file.)

# Important: Please Read!
1. If you need help, please feel free to get in touch with me on [Twitter](https://twitter.com/acid_rain), or open a new issue if it doesn't already exist. Please ensure you specify which version of Slackord2 you are using, and the Operating System you're running Slackord2 on.
1. I have decided against importing multiple JSON files to disincentivise using Slackord2 maliciously. You can, however, merge multiple JSON files into one with [this tool](https://tools.knowledgewalls.com/onlinejsonmerger) or many others that exist online which will achieve the same result.
1. Slackord2 is free, and it will always be free. However, if you found Slackord2 worth donating something, you can donate from within Slackord2 by accessing "Donate" from the "Help" context menu, you can sponsor Slackord2 at the top of this repository by clicking the heart (sponsor) button, or [click here](https://paypal.me/thomasloupe)(PayPal) to donate directly.
