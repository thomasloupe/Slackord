# ![Slackord Logo](https://i.imgur.com/PyVjqzL.png)Slackord
![Discord Shield](https://discordapp.com/api/guilds/1095636526873972766/widget.png?style=shield)    
[Join the Slackord Discord!](https://discord.gg/yccMweYPN8)

Slackord is a cross-platform application that parses JSON chat history file exports from Slack and posts them into Discord instantly with a single command.
Slackord is the .NET7 MAUI version of the [Slackord 1.x](https://github.com/thomasloupe/Slackord1) Python/Tkinter app with significant feature additions and improvements.

# Demo
## Windows
https://user-images.githubusercontent.com/6563450/188337355-41fdc913-2b9f-41c3-9824-87986bb4d792.mp4
## Linux/Mac
https://user-images.githubusercontent.com/6563450/188337369-10823d19-5bce-42d8-a2b0-0fd8b8f5a8bc.mp4

# Features
1. `Cross platform` - Works on Windows(x64) and Mac(x64).
1. `Imports channels` - Slackord will automatically recreate all your Slack channels on Discord.
1. `Batch parsing/posting` - Instead of importing JSON files individually, Slackord will read your entire Slack export directory for JSON files parse/post them to Discord.
1. `Output window` - Slackord has a log window that will update you on what's going on in real-time.
1. `Progress Bar` - Slackord has a progress bar and will tell you how many messages in total there are to send to Discord, and how many it's already sent.
1. `Auto rate-limit detection` - Slackord limits the messages it sends over time so it doesn't spam or get itself squelched for posting too often.
1. `Ease-of-use` - Slackord only needs to be set up once, and it'll remember your bot's token, making future imports easy.
1. `Multiple Discord Servers` - Slackord can discern which Discord server it's in and can post to the server(s) you choose.
1. `Privacy first` - Slackord checks if user messages have a display name and will attempt to keep real names private unless there isn't one.
1. `Data safety` - Your data is yours. Slackord works completely off your local connection and machine.
1. `Update checks` - Slackord has the ability to check for updates and. Get the latest version with new features and fixes easily!

# Getting Started
If you are on the free plan of Slack or need a reliable tool for exporting both private/public Slack JSON files *mostly* compatible with Slackord, check out [Slackdump](https://github.com/rusq/slackdump)! Please note, this tool is not developed by myself, and could potentially stop working for Slackord at any time.
1. Download the [latest](https://github.com/thomasloupe/Slackord/releases) Slackord release for your OS and extract the contents. If you're on Linux/Mac, make sure to `cd` into the Slackord directory you extracted, and grant execute permissions to the directory with `chmod +x *` from the CLI/Terminal.
1. Create a Discord bot [here](https://discord.com/developers/applications) by selecting "New Application" at the top-right.
1. Name your bot "Slackord", or any preferred custom name.
1. Select "Bot" from the left panel, and click "Add Bot" at the top-right.
1. Under OAuth2>URL Generator, set the bot's "SCOPES" to "bot". This opens a new menu called "BOT PERMISSIONS" below.
1. In "BOT PERMISSIONS", set the bot's permissions to "Administrator". This allows Slackord to post to private channels, too.
1. Copy the "GENERATED URL" link below the "BOT PERMISSIONS", and paste into a browser.
1. Join the bot into your desired server.
1. In the "Bot" page underneath "OAuth2", Upload an image for your bot if desired.
1. Click "Reset Token", select "Yes, do it!", then click the "Copy" button to the left of "Regenerate". "Keep it secret, keep it safe."
1. Ensure that both "PUBLIC BOT" and "REQUIRES OAUTH2 CODE GRANT" sliders are turned off.
1. In "Privileged Gateway Intents", tick the slider to enable "MESSAGE CONTENT INTENT".

# Running Slackord for Windows
1. Run Slackord.
1. Click `Set Bot Token` and paste the copied token into the popup text field. Slackord will remember your token if you close it.
1. Click `Connect` press to connect the bot to your server.
1. Click `Import JSON` and select the Slack JSON chat history root folder.
1. Once parsing has completed, visit any Discord channel you wish to updates from Slack chat history to and type `/slackord` (case insensitive). Messages will begin posting.

# Important: Please Read!
1. If you need help, please feel free to join the Discord community listed at the top of this page, or open a new issue if it doesn't already exist.
1. Slackord is free, and it will always be free. However, if you found Slackord worth donating something, you can donate from within Slackord by clicking Donate inside Slackord, or you can sponsor Slackord at the top of this repository by clicking the heart (sponsor) button. You can also [click here](https://paypal.me/thomasloupe) (PayPal) to donate directly to me.