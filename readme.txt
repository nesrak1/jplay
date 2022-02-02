Reupload of JAudio for Nintendo BMS files (http://jplay.codeplex.com/)

Originally hosted on Codeplex but removed once Codeplex shut down.

Original readme
===============

JAudio Player is a player for BMS music sequences that are used in several games for Nintendo GameCube and Wii.

**I do not own this format.** The JAudio library belongs to Nintendo, this is just a player therefore.

This is a first preview version of the JAudio Player that has already basic functionality. Some important things like envelope data and decoding the sound files are not supported yet, furthermore it has many bugs.

Missing features/issues

    Envelope data
    Decoding of the .aw files
    Looping
    Vibrato
    Dynamic parts/tracks
    Reverb is too silent
    Playback issues in certain tracks
    Percussion has wrong pan
    Some memory and performance issues

The player needs the extracted/decoded .wav files to work (they have to be in the same folder as the .aw files). Use wsyster for extracting the sounds.
===============

This program plays BMS sequences. It has a few modifications to the UI that
are not present in the original such as instrument selection, but the BMS
playing code is still the same. It was unfinished and as such, some
instruments may sound wrong such as pitch adjustments or have sample
clipping issues.

The sample extraction code was written for Zelda Twilight Princess.
To get the files, load the folder of the game in dolphin, then right click
and select Properties. Select the Filesystem tab and export Z2Sound.baa and
the Waves folder from Disc/Partition 1/Audiores to somewhere on your computer.

Once you start JAudio, select the folder where you exported these two.
After the application unfreezes, you can now open bms files. You can get
these by running yaz0dec and rarcdump on the Audiores/Seqs/Z2SoundSeqs.arc
file. These two tools are part of szstools at
http://www.amnoid.de/gc/szstools.zip