# ![Slackord Logo](https://i.imgur.com/PyVjqzL.png)Slackord
![Discord Shield](https://discordapp.com/api/guilds/1095636526873972766/widget.png?style=shield)    
[Join the Slackord Discord!](https://discord.gg/yccMweYPN8)

Slackord is an application that parses JSON chat history file exports from Slack and posts them into Discord instantly with a single command.

# Demo
https://github.com/user-attachments/assets/afa63789-0904-4200-8bd8-6c5dd1970355

# Features
1. `Recreate channels` - Slackord will automatically recreate all your Slack channels on Discord along with their descriptions.
1. `Resume Functionality` - Slackord can resume partial server and channel imports.
1. `Batch and Single channel compatibility` - Choose between importing a single channel or your entire Slack server into Discord.
1. `Output window` - Slackord has a log window that keeps you updated on what's happening in real-time.
1. `Progress bar` - Slackord has a progress bar that keeps you updated on parsing and posting progress.
1. `Slack-to-Discord markdown` - Slackord will convert messages with Slack markdown to Discord markdown.
1. `Auto rate-limit detection` - Slackord limits the messages it sends over time so it doesn't spam or get itself squelched for posting too often.
1. `Ease-of-use` - Slackord only needs to be set up once, making future imports easy. It also remembers your bot's token.
1. `Multi-Server Compatibility` - Join Slackord into a test server and your live server at the same time, and test your imports.
1. `Privacy & Data Safety` - Slackord allows you to set the name format and fallback when posting messages. Choose between `display name`, `user name`, and `real name`. In addition to user privacy, your data is yours. Slackord works completely off your local connection and machine.
1. `Update checks` - Slackord allows you to toggle the ability to get notified when a new version is available.
1. `Active development` - Slackord is actively maintained with consistent quality-of-life fixes, bugfixes, and new features.
1. `Slackdump Compatibility` - Slackord supports Slackdump exports [Slackdump](https://github.com/rusq/slackdump).

# Slackord Does Not Support
1. Private Direct Messages - Since Slackord is meant for Slack Workspace Admins and not end-users, private DMs will not be supported due to user privacy concerns.
1. Reactions - A bot cannot give a post ten thumbs up emojis. These actions are done by individuals, which makes it very difficult to recreate accurately. However, I'm open to ideas via issue submissions and sponsorship.
1. Mapping Slack users to Discord users - Support for this may be considered if sponsored, but will likely be offered as a separate tool.

# Getting Started
1. Download the [latest](https://github.com/thomasloupe/Slackord/releases) Slackord release and extract the contents.
2. Create a Discord bot [here](https://discord.com/developers/applications) by selecting "New Application" at the top-right.
3. Name your bot "Slackord", or any preferred custom name.
4. On the "Installation" page, select "Guild Install", and select "None" for the "Install Link" and save your changes.
5. Select "Bot" from the left panel. Ensure that both "Public Bot", and "Requires OAuth2 Code Grant" are both toggled off in Authorization Flow, and "Message Content Intent" is toggled on. then save your changes.
6. Select "OAuth2" from the left panel. set the bot's "SCOPES" to "bot". This opens a new menu called "BOT PERMISSIONS" below. Set the bot's permissions to "Administrator".
7. Copy the "GENERATED URL" link below the "BOT PERMISSIONS", and paste into a browser. Join the bot into your desired Discord server.
9. Click "Reset Token", select "Yes, do it!", then click the "Copy" button to the left of "Regenerate". "Keep it secret, keep it safe."](https://jenkins-prod.zenoss.io/job/Zing_Production_Deployment_for_zcloud-prod3/job/update-and-deploy/186/console)

# Running Slackord
1. Run Slackord. You may need to install the latest .NET9 framework [here](https://builds.dotnet.microsoft.com/dotnet/Runtime/9.0.5/dotnet-runtime-9.0.5-win-x64.exe).
1. Click `Options` and paste your Discord bot's token into the `Bot Token` text field.
1. Click `Connect` to connect the bot to your server.
1. Click `Import Server` and select the Slack JSON chat history root folder, or `Import Channel` and select the channel folder.
1. Once parsing has completed, visit any Discord channel you wish and type `/slackord`. Messages will begin posting and the progress bar will update as messages are sent.
1. At any point, you can disconnect or close Slackord and later resume your import. To resume an import, use the `/slackord` or `/resume` slash command when prompted or if Slackord is still running, after reconnecting the bot to Discord.

# Need Help?
1. Please join the Discord community listed at the top of this page, or open a new issue if it doesn't already exist.
1. If you'd like to donate, you can do so from within Slackord by opening the `Options` page and clicking the `Donate` button at the bottom of the page, sponsoring Slackord at the top of this repository by clicking the heart (sponsor) button, or donating directly to me via [PayPal](https://paypal.me/thomasloupe).

# Special Mentions
While Slackord handles migrating your Slack, archiving your Slack history is something you might also want to do. Check out [Slackdump](https://github.com/rusq/slackdump) for archiving your entire Slack workspace! Slackord officially supports Slackdump exports.
