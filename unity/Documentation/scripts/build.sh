rm -rf clientHTMLOutput
rm -rf client

docfx metadata
docfx build docfx.json > docfx.log

if [ $? -ne 0 ]; then
cat docfx.log
echo "Error: docfx build failed." >&2
exit $?
fi

cat docfx.log

if ! grep -q "Build succeeded." docfx.log; then
echo "There are build warnings." >&2
fi
