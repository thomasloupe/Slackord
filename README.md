# Slackord2 ![Slackord2 Logo](https://i.imgur.com/PyVjqzL.png)

Slackord2 is a cross-platform application that parses JSON chat history file exports from Slack and posts them into Discord instantly with a single command.
Slackord2 is the .NET7 version of the [Slackord 1.x](https://github.com/thomasloupe/Slackord) Python/Tkinter app with significant feature additions and improvements.

# Demo
## Windows
https://user-images.githubusercontent.com/6563450/188337355-41fdc913-2b9f-41c3-9824-87986bb4d792.mp4
## Linux/Mac
https://user-images.githubusercontent.com/6563450/188337369-10823d19-5bce-42d8-a2b0-0fd8b8f5a8bc.mp4

# Features
1. `Cross platform` - Works on Windows(x64), Mac(x64), and Linux(x64).
1. `Post everything` - Post messages, images, and attachments to any Discord channel or Direct Message conversation.
1. `Batch parsing/posting` - Instead of importing JSON files individually, Slackord2 can read all JSON files within a directory and parse/post them to Discord.
1. `Robust Debug window` - Slackord2 lets you know exactly what's happening in real-time. You'll know what messages are being posted, how they'll be formatted, and what the bot is doing before sending messages to their destination channels.
1. `Rate limit detection` - Slackord2 limits the messages it sends over time so it doesn't spam or get itself squelched for posting too often.
1. `Ease-of-use` - Slackord2 only needs to be set up once, and it'll remember your bot's token, making future imports easy.
1. `Privacy first` - Slackord2 checks if user messages have a display name and will attempt to keep real names private unless there isn't one.
1. `Data safety` - Your data is yours. Slackord2 works completely off your local connection and machine.
1. `Update checks` - Slackord2 has the ability to check for updates in the `Help` context menu. Get the latest version with new features easily!

# Getting Started
If you are on the free plan of Slack or need a reliable tool for exporting both private/public Slack JSON files compatible with Slackord2, check out [Slackdump](https://github.com/rusq/slackdump)!
1. Download the [latest](https://github.com/thomasloupe/Slackord2/releases) Slackord2 release for your OS and extract the contents. If you're on Linux/Mac, make sure to `cd` into the Slackord2 directory you extracted, and grant execute permissions to the directory with `chmod +x *` from the CLI/Terminal.
1. Create a Discord bot [here](https://discord.com/developers/applications) by selecting "New Application" at the top-right.
1. Name your bot "Slackord2", or any preferred custom name.
1. Under OAuth2>URL Generator, set the bot's "SCOPES" to "bot". This opens a new menu called "BOT PERMISSIONS" below.
1. In "BOT PERMISSIONS", set the bot's permissions to "Administrator". This allows Slackord2 to post to private channels, too.
1. Copy the "GENERATED URL" link below the "BOT PERMISSIONS", and paste into a browser.
1. Join the bot into your desired server.
1. In the "Bot" page underneath "OAuth2", Upload an image for your bot if desired.
1. Click "Reset Token", select "Yes, do it!", then click the "Copy" button to the left of "Regenerate". "Keep it secret, keep it safe."
1. Ensure that both "PUBLIC BOT" and "REQUIRES OAUTH2 CODE GRANT" sliders are turned off.
1. In "Privileged Gateway Intents", tick the slider to enable "MESSAGE CONTENT INTENT".
1. Follow the instructions below based on which Operating System you downloaded Slackord2 for.

# Running Slackord2 for Windows
1. Run Slackord2.
1. Select `Settings>Enter Bot Token` and paste the copied token into the text field. Slackord2 will remember your token if you close it.
1. Select `File>Import JSON` or `File>Import JSON Folder` and select a Slack JSON chat history file or folder to import.
1. Select `Settings>Bot Connection>Connect` to connect the bot to your server.
1. Visit the Discord channel or DM you wish to import Slack chat history to and type `/slackord`.
1. Messages will begin posting.

# Running Slackord2 for Mac/Linux
1. Run Slackord2 and paste your bot's token into the CLI when prompted to do so. Slackord2 will remember your token if you close it.
1. Place any Slack JSON files you wish to parse inside of the `Files` directory.
1. Parsing will begin and the bot will connect to your Discord server.
1. Visit the Discord channel or DM you wish to import Slack chat history to and type `/slackord` (case insensitive).
1. Messages will begin posting.

# Important: Please Read!
1. If you need help, please feel free to get in touch with me on [Twitter](https://twitter.com/acid_rain), or open a new issue if it doesn't already exist. Please ensure you specify which version of Slackord2 you are using, and the Operating System you're running Slackord2 on.
1. Slackord2 is free, and it will always be free. However, if you found Slackord2 worth donating something, you can donate from within Slackord2 by accessing "Donate" from the "Help" context menu, from within the CLI "About" section, or you can sponsor Slackord2 at the top of this repository by clicking the heart (sponsor) button. You can also [click here](https://paypal.me/thomasloupe) (PayPal) to donate directly to me.
