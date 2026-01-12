# Frequently Asked Questions

## General questions

### Q: I am having issues with Slackord, what should I do?
A: Please post an [issue on Github](https://github.com/thomasloupe/Slackord/issues) or join the community Discord, but please first read the [troubleshooting guide](https://github.com/thomasloupe/Slackord2/wiki/Troubleshooting).

### Q: Why don't you call your tool "SlackLord"?
A: See the [glossary](glossary.md) for the meaning of Slackord.

### Q: Where can I find documentation?
A: You're looking at it! This documentation site contains:
- [Glossary](glossary.md) - Key terms and definitions
- This FAQ
- [API Reference](../api/index.md) - Complete API documentation

### How do I get started with Slackord?
A: Start using Slackord by visiting the [GitHub README](https://github.com/thomasloupe/Slackord).

### Q: How do I contribute to Slackord?
A: If you'd like to contribute to Slackord, you can raise an issue about the contribution, then create a PR and link it to your issue for review.

### Q: I see repeating questions in Discord, can I assist answering them?
A: Yes, feel free to do so within the Discord code of conduct, or use a link to this FAQ when it's applicable to the question.

## Technical questions

### Q: I see `The application did not respond` in my Discord channel. What's wrong?
A: This is either an automated timeout in discord (20 minutes) and can be ignored or, it may indicate that Discord can't reach the local Slackord client - make sure it's still running and connected.

### Q: What causes Rate Limiting messages in the Slackord app, what's happening?
A: You can safely ignore this. Rate limiting is imposed on all apps to prevent spamming the Discord API. The Slackord client will backoff and resume posting automatically to prevent rate limiting.

### Q: I keep seeing: `Gateway: A SlashCommandExecuted handler is blocking the gateway task`. Is something wrong?
A: As long as messages are still posting, you can ignore the warning.
