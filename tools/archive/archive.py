import shutil
import sys

shutil.make_archive(sys.argv[1], "zip", sys.argv[2], sys.argv[3])
