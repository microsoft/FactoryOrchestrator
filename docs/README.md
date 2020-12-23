# Factory Orchestrator Documentation

[Factory Orchestrator](https://microsoft.github.io/FactoryOrchestrator/) uses [MkDocs](https://www.mkdocs.org/) to generate usage documentation.

We welcome contributions on our documentation as well under the same code of conduct as the rest of the project.

To edit and locally view the docs you can follow [these steps](https://www.mkdocs.org/#building-the-site):

## One-time setup:

1. Install python
2. From this directory (\docs) run: pip install --upgrade -r requirements.txt

## Viewing changes locally
1. From this directory (\docs) run: mkdocs serve

## Commiting changes to the live site
Once you are happy with your changes, you will have to manually update the live docs by doing the following from the root folder (TODO: make part of the CI build):

   1. Checkout a working branch based on the latest code in 'main'.
   2. Make your desired documentation changes.
   3. mkdocs build --clean --config-file .\docs\mkdocs.yml
   4. git checkout gh-pages
   5. robocopy /S .\docs\site\ .\
   6. git checkout -b <your working branch>
   7. Commit & push your changes, open a pull request into 'gh-pages'. Once approved and the remote 'gh-pages' has the changes it will update the website automatically.

To help prevent documentation<->code mismatches, the GitHub PR build will detect if your code changes will result in any documentation updates. If documentation updates are needed, the build will generate a build warning and publish a Git .patch file. The .patch file can be used to make a PR into 'gh-pages' in lieu of the steps above.

Please note that any changes to the public API surface of the Microsoft.FactoryOrchestrator.Client and/or Microsoft.FactoryOrchestrator.Core classes will result in documentation changes. If you see your changes result in modified files in docs\docs, you must run the above steps to rebuild the website source manually!

## Open Source Software Acknowledgments

### Mkdocs

<https://www.mkdocs.org/>

MkDocs License (BSD)
Copyright © 2014, Tom Christie. All rights reserved.
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

### Material for MkDocs

<https://squidfunk.github.io/mkdocs-material/>

License
MIT License
Copyright © 2016 - 2017 Martin Donath
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
