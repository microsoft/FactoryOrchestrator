# Factory Orchestrator Documentation

[Factory Orchestrator](https://microsoft.github.io/FactoryOrchestrator/) uses [MkDocs](https://www.mkdocs.org/) to generate usage documentation.

We welcome contributions on our documentation as well under the same code of conduct as the rest of the project.

To build & locally view the docs you can follow [these steps](https://www.mkdocs.org/#building-the-site):

From the root:

1. pip install --upgrade -r requirements.txt
2. mkdocs build --clean
3. mkdocs serve
   
Once you are happy with the changes, you can prepare a pull request to update the checked in docs as you would with any other code change. 

When completed, then you will have to manually update the live docs by do the following from the root (todo: make better):

   1. (optional- in case you rev'd it) mkdocs build --clean --config-file .\docs\mkdocs.yml 
   2. git checkout gh-pages
   3. git checkout -b <topic branch with>
   4. Commit your changes, open a pull request. Once approved and the remote 'gh-branch' has the last changes then it will update the website.

## ## Open Source Software Acknowledgments

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
