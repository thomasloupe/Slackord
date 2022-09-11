ls | xargs -I % sh -c 'cd %; ls | xargs cat | sed "s/\]\[/,/g" > ../../Slackord2_Linux_x64/Files/%.json; cd ..'
