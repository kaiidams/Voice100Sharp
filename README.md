# Voice100 for C#

[![Binder](https://mybinder.org/badge_logo.svg)](https://mybinder.org/v2/gh/kaiidams/Voice100Sharp/main)

## Quick start

Clone the repository. Make sure to enable
[Git LFS](https://git-lfs.github.com/)
as WAV files are stored in LFS.

```s
git clone https://github.com/kaiidams/Voice100Sharp.git
```

Visual Studio, you can open
`Voice100Sharp\Voice100Sharp.sln`, set `Voice100SharpApp` to the default
project, and run the program with `F5`.

The program runs one of several tests, given by the command argument.

In the `voice100` test, it speaks once at the start up and then listens to
the microphone and prints the recognized texts.

In the `test` test, reads a test file,
`test_data\transcript.txt` and print predicted results. The format of the output is
three columns separated by `|`, names of wav files, target texts, predicted texts.
The test data is from
[test-clean.tar.gz](https://www.openslr.org/resources/12/test-clean.tar.gz)
of
[LibriSpeech](https://www.openslr.org/12/).

In both mode, the program downloads required ONNX files from GitHub.

Name | Target | Predicted
--- | --- | ---
61-70968-0000.wav|he began a confused complaint against the wizard who had vanished behind the curtain on the left|he began o confused complaint against the wizard who hadvanish behind the curti on the left
61-70968-0001.wav|give not so earnest a mind to these mummeries child|kihave not so earnest a mine to these mummaris child
61-70968-0002.wav|a golden fortune and a happy life|a golden fortune an a happy life
61-70968-0003.wav|he was like unto my father in a way and yet was not my father|he was like u t y father an away and yet was not my father
61-70968-0004.wav|also there was a stripling page who turned into a maid|also there was a strippling page who turned it to a mad
61-70968-0005.wav|this was so sweet a lady sir and in some manner i do think she died|this was so swet lady sir and and some manter i do think she died
61-70968-0006.wav|but then the picture was gone as quickly as it came|but then the picture was gone as quickly as it came
61-70968-0007.wav|sister nell do you hear these marvels|sister nell dyu hear these mavels
61-70968-0008.wav|take your place and let us see what the crystal can show to you|take your place and let us see wit the crystal can show to you
61-70968-0009.wav|like as not young master though i am an old man|like is not youg master though i am an old man
61-70968-0010.wav|forthwith all ran to the opening of the tent to see what might be amiss but master will who peeped out first needed no more than one glance|forthwith all rant the opening of the tent to see what might be amiss but master will who peped out first needed kno more than one glance
61-70968-0011.wav|he gave way to the others very readily and retreated unperceived by the squire and mistress fitzooth to the rear of the tent|he gave wy to the others very readily and retreated unperceived by the squire in mistress fitzoth to the rear of the tent
61-70968-0012.wav|cries of a nottingham a nottingham|cries of a notting cam on not acam
61-70968-0013.wav|before them fled the stroller and his three sons capless and terrified|before them floed the stroler and his three sons capless and terrified
61-70968-0014.wav|what is the tumult and rioting cried out the squire authoritatively and he blew twice on a silver whistle which hung at his belt|what is the tumuletan rioting cried out the squire authoritatively and he blew twice on the silver whistle which ong at is belt
61-70968-0015.wav|nay we refused their request most politely most noble said the little stroller|nay we refrezed the request most politely nost noble said the little stroller
61-70968-0016.wav|and then they became vexed and would have snatched your purse from us|and then they became vexed and would av snahe your persram us
61-70968-0017.wav|i could not see my boy injured excellence for but doing his duty as one of cumberland's sons|i could not see my boy injured excellencse for but doing his dutias one of cumberl and sons
61-70968-0018.wav|so i did push this fellow|so i did push this fellow
61-70968-0019.wav|it is enough said george gamewell sharply and he turned upon the crowd|it is enough said george game well sharply as he turned upon the crowd
61-70968-0020.wav|shame on you citizens cried he i blush for my fellows of nottingham|she on us citizens crie he i lish for my fellows of not ahim
61-70968-0021.wav|surely we can submit with good grace|surely we can submit with good grace
61-70968-0022.wav|tis fine for you to talk old man answered the lean sullen apprentice|tis find y you do tolk gold man answered the lean soinaprintec
61-70968-0023.wav|but i wrestled with this fellow and do know that he played unfairly in the second bout|but i ristled with this fellow and do know that he played un fairly in the second bout
61-70968-0024.wav|spoke the squire losing all patience and it was to you that i gave another purse in consolation|spoke the squire losing all patient and it was t you that i gave ather person consolation
61-70968-0025.wav|come to me men here here he raised his voice still louder|come to me men here here he raised his voice till louder
61-70968-0026.wav|the strollers took their part in it with hearty zest now that they had some chance of beating off their foes|the strolers took their partinit with harties askd now hat they had some chance of beating off their foes
61-70968-0027.wav|robin and the little tumbler between them tried to force the squire to stand back and very valiantly did these two comport themselves|robit in the little temper between them tried to force the squire to stand back and very valieatly to these to comport themselves
61-70968-0028.wav|the head and chief of the riot the nottingham apprentice with clenched fists threatened montfichet|the headen chief of the riot denodting hama printic with quench fists threatened montyche
61-70968-0029.wav|the squire helped to thrust them all in and entered swiftly himself|the squire helped to thrust them alland and enterd twifly himself
61-70968-0030.wav|now be silent on your lives he began but the captured apprentice set up an instant shout|now be silent on your lies he began but the capturedo printic set up in instant shout
61-70968-0031.wav|silence you knave cried montfichet|silence you nave cried montryshe
61-70968-0032.wav|he felt for and found the wizard's black cloth the squire was quite out of breath|he felt for an found the wistrd's black cloth the squire was quite out of breath
61-70968-0033.wav|thrusting open the proper entrance of the tent robin suddenly rushed forth with his burden with a great shout|thrusting opend the proper entrance of the tent robin suddenly rushed forth with his burden with a great shout
61-70968-0034.wav|a montfichet a montfichet gamewell to the rescue|amof fica a motychee game well to the recue
61-70968-0035.wav|taking advantage of this the squire's few men redoubled their efforts and encouraged by robin's and the little stroller's cries fought their way to him|taking advantage of this tesquires fu men re double their efforts and encouraged by robins in the little strollers cries faughtheir way to him
61-70968-0036.wav|george montfichet will never forget this day|george montfy she will never forget this day
61-70968-0037.wav|what is your name lording asked the little stroller presently|what is your name lording asked the little stroller presently
61-70968-0038.wav|robin fitzooth|ropin fitzfh
61-70968-0039.wav|and mine is will stuteley shall we be comrades|and mine as willl stuly shall we becomrade
61-70968-0040.wav|right willingly for between us we have won the battle answered robin|right willingly for between us we have one the bettle answered brobin
61-70968-0041.wav|i like you will you are the second will that i have met and liked within two days is there a sign in that|i like you will you are the second well that i have met en likhtd within two days is there is sign in that
61-70968-0042.wav|montfichet called out for robin to give him an arm|monty she called out for robin to give him an arm
61-70968-0043.wav|friends said montfichet faintly to the wrestlers bear us escort so far as the sheriff's house|friends said montveishe faintly to the restlers barrus escort so far as the surof's house
61-70968-0044.wav|it will not be safe for you to stay here now|it will not be safe for you to stay here now
61-70968-0045.wav|pray follow us with mine and my lord sheriff's men|pray follow us with mine in my lord sherismen
61-70968-0046.wav|nottingham castle was reached and admittance was demanded|not a aun castle was reached and imittence was demanded
61-70968-0047.wav|master monceux the sheriff of nottingham was mightily put about when told of the rioting|master monse the shereif o knodding ham was nightly put about hand told of the riting
61-70968-0048.wav|and henry might return to england at any moment|and henry might return to england at any moment
61-70968-0049.wav|have your will child if the boy also wills it montfichet answered feeling too ill to oppose anything very strongly just then|have your will child if the boy also will i monte shat answered feeling two ill to oppose anything very strongly just then
61-70968-0050.wav|he made an effort to hide his condition from them all and robin felt his fingers tighten upon his arm|he made an effort to hid hes condition from them all and robin felt his fingers tighten upon his arm
61-70968-0051.wav|beg me a room of the sheriff child quickly|bage me a room of the sherif child quickly
61-70968-0052.wav|but who is this fellow plucking at your sleeve|but who is is fellow puckyet wer speed
61-70968-0053.wav|he is my esquire excellency returned robin with dignity|he as my esqui excellency returned robin with dignity
61-70968-0054.wav|mistress fitzooth had been carried off by the sheriff's daughter and her maids as soon as they had entered the house so that robin alone had the care of montfichet|mistress fitteth had been carried off we the sarif daughter and her maids as soon as they had entered the house o that roben alone had the care of move shee
61-70968-0055.wav|robin was glad when at length they were left to their own devices|ropin was glad whand at length they were left ho their own devices
61-70968-0056.wav|the wine did certainly bring back the color to the squire's cheeks|the wind id certainly bring back to color to the squires cheeks
61-70968-0057.wav|these escapades are not for old gamewell lad his day has come to twilight|these escapates are not for old came well lad his day has come to twilight
61-70968-0058.wav|will you forgive me now|will you forgive me now
61-70968-0059.wav|it will be no disappointment to me|it will be no disappointment to me
61-70968-0060.wav|no thanks i am glad to give you such easy happiness|no thanks i am glad to give you such easy happiness
61-70968-0061.wav|you are a worthy leech will presently whispered robin the wine has worked a marvel|you are worthyyu leak will presently whispered roben the wine his worked a marfel
61-70968-0062.wav|ay and show you some pretty tricks|i an show you ome pretty tricks

## Build unmanaged binaries

### Build WORLD for Windows

Build [WORLD](http://www.kisc.meiji.ac.jp/~mmorise/world/english) outside this repository.

```s
git clone https://github.com/mmorise/World.git
md World\build
md World\build
cmake .. -A Win32 -B x86-windows
cmake --build x86-windows --config Release
cmake .. -A x64 -B x86_64-windows
cmake --build x86_64-windows --config Release
```

### Build voice100_native for Windows

```s
md voice100_native\build
cd voice100_native\build
set WORLD_DIR=path\to\World
cmake .. -A Win32 -B win-x86 ^
    -D WORLD_INC=%WORLD_DIR%\src ^
    -D WORLD_LIB=%WORLD_DIR%\build\x86-windows
cmake --build win-x86 --config Release
cmake .. -A x64 -B win-x64 ^
    -D WORLD_INC=%WORLD_DIR%\src ^
    -D WORLD_LIB=%WORLD_DIR%\build\x86_64-windows
cmake --build win-x64 --config Release
```
