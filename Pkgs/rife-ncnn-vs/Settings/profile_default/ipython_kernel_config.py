# Configuration file for ipython-kernel.

#------------------------------------------------------------------------------
# ConnectionFileMixin(LoggingConfigurable) configuration
#------------------------------------------------------------------------------

## Mixin for configurable classes that work with connection files

## JSON file in which to store connection info [default: kernel-<pid>.json]
#  
#  This file will contain the IP, ports, and authentication key needed to connect
#  clients to this kernel. By default, this file will be created in the security
#  dir of the current profile, but can be specified by absolute path.
#c.ConnectionFileMixin.connection_file = ''

## set the control (ROUTER) port [default: random]
#c.ConnectionFileMixin.control_port = 0

## set the heartbeat port [default: random]
#c.ConnectionFileMixin.hb_port = 0

## set the iopub (PUB) port [default: random]
#c.ConnectionFileMixin.iopub_port = 0

## Set the kernel's IP address [default localhost]. If the IP address is
#  something other than localhost, then Consoles on other machines will be able
#  to connect to the Kernel, so be careful!
#c.ConnectionFileMixin.ip = ''

## set the shell (ROUTER) port [default: random]
#c.ConnectionFileMixin.shell_port = 0

## set the stdin (ROUTER) port [default: random]
#c.ConnectionFileMixin.stdin_port = 0

## 
#c.ConnectionFileMixin.transport = 'tcp'

#------------------------------------------------------------------------------
# InteractiveShellApp(Configurable) configuration
#------------------------------------------------------------------------------

## A Mixin for applications that start InteractiveShell instances.
#  
#  Provides configurables for loading extensions and executing files as part of
#  configuring a Shell environment.
#  
#  The following methods should be called by the :meth:`initialize` method of the
#  subclass:
#  
#    - :meth:`init_path`
#    - :meth:`init_shell` (to be implemented by the subclass)
#    - :meth:`init_gui_pylab`
#    - :meth:`init_extensions`
#    - :meth:`init_code`

## Execute the given command string.
#c.InteractiveShellApp.code_to_run = ''

## Run the file referenced by the PYTHONSTARTUP environment variable at IPython
#  startup.
#c.InteractiveShellApp.exec_PYTHONSTARTUP = True

## List of files to run at IPython startup.
#c.InteractiveShellApp.exec_files = []

## lines of code to run at IPython startup.
#c.InteractiveShellApp.exec_lines = []

## A list of dotted module names of IPython extensions to load.
#c.InteractiveShellApp.extensions = []

## dotted module name of an IPython extension to load.
#c.InteractiveShellApp.extra_extension = ''

## A file to be run
#c.InteractiveShellApp.file_to_run = ''

## Enable GUI event loop integration with any of ('glut', 'gtk', 'gtk2', 'gtk3',
#  'osx', 'pyglet', 'qt', 'qt4', 'qt5', 'tk', 'wx', 'gtk2', 'qt4').
#c.InteractiveShellApp.gui = None

## Should variables loaded at startup (by startup files, exec_lines, etc.) be
#  hidden from tools like %who?
#c.InteractiveShellApp.hide_initial_ns = True

## Configure matplotlib for interactive use with the default matplotlib backend.
#c.InteractiveShellApp.matplotlib = None

## Run the module as a script.
#c.InteractiveShellApp.module_to_run = ''

## Pre-load matplotlib and numpy for interactive use, selecting a particular
#  matplotlib backend and loop integration.
#c.InteractiveShellApp.pylab = None

## If true, IPython will populate the user namespace with numpy, pylab, etc. and
#  an ``import *`` is done from numpy and pylab, when using pylab mode.
#  
#  When False, pylab mode should not import any names into the user namespace.
#c.InteractiveShellApp.pylab_import_all = True

## Reraise exceptions encountered loading IPython extensions?
#c.InteractiveShellApp.reraise_ipython_extension_failures = False

#------------------------------------------------------------------------------
# Application(SingletonConfigurable) configuration
#------------------------------------------------------------------------------

## This is an application.

## The date format used by logging formatters for %(asctime)s
#c.Application.log_datefmt = '%Y-%m-%d %H:%M:%S'

## The Logging format template
#c.Application.log_format = '[%(name)s]%(highlevel)s %(message)s'

## Set the log level by value or name.
#c.Application.log_level = 30

#------------------------------------------------------------------------------
# BaseIPythonApplication(Application) configuration
#------------------------------------------------------------------------------

## IPython: an enhanced interactive Python shell.

## Whether to create profile dir if it doesn't exist
#c.BaseIPythonApplication.auto_create = False

## Whether to install the default config files into the profile dir. If a new
#  profile is being created, and IPython contains config files for that profile,
#  then they will be staged into the new directory.  Otherwise, default config
#  files will be automatically generated.
#c.BaseIPythonApplication.copy_config_files = False

## Path to an extra config file to load.
#  
#  If specified, load this config file in addition to any other IPython config.
#c.BaseIPythonApplication.extra_config_file = ''

## The name of the IPython directory. This directory is used for logging
#  configuration (through profiles), history storage, etc. The default is usually
#  $HOME/.ipython. This option can also be specified through the environment
#  variable IPYTHONDIR.
#c.BaseIPythonApplication.ipython_dir = ''

## Whether to overwrite existing config files when copying
#c.BaseIPythonApplication.overwrite = False

## The IPython profile to use.
#c.BaseIPythonApplication.profile = 'default'

## Create a massive crash report when IPython encounters what may be an internal
#  error.  The default is to append a short message to the usual traceback
#c.BaseIPythonApplication.verbose_crash = False

#------------------------------------------------------------------------------
# IPKernelApp(BaseIPythonApplication,InteractiveShellApp,ConnectionFileMixin) configuration
#------------------------------------------------------------------------------

## IPython: an enhanced interactive Python shell.

## The importstring for the DisplayHook factory
#c.IPKernelApp.displayhook_class = 'ipykernel.displayhook.ZMQDisplayHook'

## ONLY USED ON WINDOWS Interrupt this process when the parent is signaled.
#c.IPKernelApp.interrupt = 0

## The Kernel subclass to be used.
#  
#  This should allow easy re-use of the IPKernelApp entry point to configure and
#  launch kernels other than IPython's own.
#c.IPKernelApp.kernel_class = 'ipykernel.ipkernel.IPythonKernel'

## redirect stderr to the null device
#c.IPKernelApp.no_stderr = False

## redirect stdout to the null device
#c.IPKernelApp.no_stdout = False

## The importstring for the OutStream factory
#c.IPKernelApp.outstream_class = 'ipykernel.iostream.OutStream'

## kill this process if its parent dies.  On Windows, the argument specifies the
#  HANDLE of the parent process, otherwise it is simply boolean.
#c.IPKernelApp.parent_handle = 0

#------------------------------------------------------------------------------
# Kernel(SingletonConfigurable) configuration
#------------------------------------------------------------------------------

## Whether to use appnope for compatiblity with OS X App Nap.
#  
#  Only affects OS X >= 10.9.
#c.Kernel._darwin_app_nap = True

## 
#c.Kernel._execute_sleep = 0.0005

## 
#c.Kernel._poll_interval = 0.05

#------------------------------------------------------------------------------
# IPythonKernel(Kernel) configuration
#------------------------------------------------------------------------------

## 
#c.IPythonKernel.help_links = [{'text': 'Python Reference', 'url': 'https://docs.python.org/3.6'}, {'text': 'IPython Reference', 'url': 'https://ipython.org/documentation.html'}, {'text': 'NumPy Reference', 'url': 'https://docs.scipy.org/doc/numpy/reference/'}, {'text': 'SciPy Reference', 'url': 'https://docs.scipy.org/doc/scipy/reference/'}, {'text': 'Matplotlib Reference', 'url': 'https://matplotlib.org/contents.html'}, {'text': 'SymPy Reference', 'url': 'http://docs.sympy.org/latest/index.html'}, {'text': 'pandas Reference', 'url': 'https://pandas.pydata.org/pandas-docs/stable/'}]

## Set this flag to False to deactivate the use of experimental IPython
#  completion APIs.
#c.IPythonKernel.use_experimental_completions = True

#------------------------------------------------------------------------------
# InteractiveShell(SingletonConfigurable) configuration
#------------------------------------------------------------------------------

## An enhanced, interactive shell for Python.

## 'all', 'last', 'last_expr' or 'none', 'last_expr_or_assign' specifying which
#  nodes should be run interactively (displaying output from expressions).
#c.InteractiveShell.ast_node_interactivity = 'last_expr'

## A list of ast.NodeTransformer subclass instances, which will be applied to
#  user input before code is run.
#c.InteractiveShell.ast_transformers = []

## Make IPython automatically call any callable object even if you didn't type
#  explicit parentheses. For example, 'str 43' becomes 'str(43)' automatically.
#  The value can be '0' to disable the feature, '1' for 'smart' autocall, where
#  it is not applied if there are no more arguments on the line, and '2' for
#  'full' autocall, where all callable objects are automatically called (even if
#  no arguments are present).
#c.InteractiveShell.autocall = 0

## Autoindent IPython code entered interactively.
#c.InteractiveShell.autoindent = True

## Enable magic commands to be called without the leading %.
#c.InteractiveShell.automagic = True

## The part of the banner to be printed before the profile
#c.InteractiveShell.banner1 = "Python 3.6.5 (v3.6.5:f59c0932b4, Mar 28 2018, 17:00:18) [MSC v.1900 64 bit (AMD64)]\nType 'copyright', 'credits' or 'license' for more information\nIPython 6.4.0 -- An enhanced Interactive Python. Type '?' for help.\n"

## The part of the banner to be printed after the profile
#c.InteractiveShell.banner2 = ''

## Set the size of the output cache.  The default is 1000, you can change it
#  permanently in your config file.  Setting it to 0 completely disables the
#  caching system, and the minimum value accepted is 3 (if you provide a value
#  less than 3, it is reset to 0 and a warning is issued).  This limit is defined
#  because otherwise you'll spend more time re-flushing a too small cache than
#  working
#c.InteractiveShell.cache_size = 1000

## Use colors for displaying information about objects. Because this information
#  is passed through a pager (like 'less'), and some pagers get confused with
#  color codes, this capability can be turned off.
#c.InteractiveShell.color_info = True

## Set the color scheme (NoColor, Neutral, Linux, or LightBG).
#c.InteractiveShell.colors = 'Neutral'

## 
#c.InteractiveShell.debug = False

## Don't call post-execute functions that have failed in the past.
#c.InteractiveShell.disable_failing_post_execute = False

## If True, anything that would be passed to the pager will be displayed as
#  regular output instead.
#c.InteractiveShell.display_page = False

## (Provisional API) enables html representation in mime bundles sent to pagers.
#c.InteractiveShell.enable_html_pager = False

## Total length of command history
#c.InteractiveShell.history_length = 10000

## The number of saved history entries to be loaded into the history buffer at
#  startup.
#c.InteractiveShell.history_load_length = 1000

## 
#c.InteractiveShell.ipython_dir = ''

## Start logging to the given file in append mode. Use `logfile` to specify a log
#  file to **overwrite** logs to.
#c.InteractiveShell.logappend = ''

## The name of the logfile to use.
#c.InteractiveShell.logfile = ''

## Start logging to the default log file in overwrite mode. Use `logappend` to
#  specify a log file to **append** logs to.
#c.InteractiveShell.logstart = False

## 
#c.InteractiveShell.object_info_string_level = 0

## Automatically call the pdb debugger after every exception.
#c.InteractiveShell.pdb = False

## Deprecated since IPython 4.0 and ignored since 5.0, set
#  TerminalInteractiveShell.prompts object directly.
#c.InteractiveShell.prompt_in1 = 'In [\\#]: '

## Deprecated since IPython 4.0 and ignored since 5.0, set
#  TerminalInteractiveShell.prompts object directly.
#c.InteractiveShell.prompt_in2 = '   .\\D.: '

## Deprecated since IPython 4.0 and ignored since 5.0, set
#  TerminalInteractiveShell.prompts object directly.
#c.InteractiveShell.prompt_out = 'Out[\\#]: '

## Deprecated since IPython 4.0 and ignored since 5.0, set
#  TerminalInteractiveShell.prompts object directly.
#c.InteractiveShell.prompts_pad_left = True

## 
#c.InteractiveShell.quiet = False

## 
#c.InteractiveShell.separate_in = '\n'

## 
#c.InteractiveShell.separate_out = ''

## 
#c.InteractiveShell.separate_out2 = ''

## Show rewritten input, e.g. for autocall.
#c.InteractiveShell.show_rewritten_input = True

## Enables rich html representation of docstrings. (This requires the docrepr
#  module).
#c.InteractiveShell.sphinxify_docstring = False

## 
#c.InteractiveShell.wildcards_case_sensitive = True

## Switch modes for the IPython exception handlers.
#c.InteractiveShell.xmode = 'Context'

#------------------------------------------------------------------------------
# ZMQInteractiveShell(InteractiveShell) configuration
#------------------------------------------------------------------------------

## A subclass of InteractiveShell for ZMQ.

#------------------------------------------------------------------------------
# ProfileDir(LoggingConfigurable) configuration
#------------------------------------------------------------------------------

## An object to manage the profile directory and its resources.
#  
#  The profile directory is used by all IPython applications, to manage
#  configuration, logging and security.
#  
#  This object knows how to find, create and manage these directories. This
#  should be used by any code that wants to handle profiles.

## Set the profile location directly. This overrides the logic used by the
#  `profile` option.
#c.ProfileDir.location = ''

#------------------------------------------------------------------------------
# Session(Configurable) configuration
#------------------------------------------------------------------------------

## Object for handling serialization and sending of messages.
#  
#  The Session object handles building messages and sending them with ZMQ sockets
#  or ZMQStream objects.  Objects can communicate with each other over the
#  network via Session objects, and only need to work with the dict-based IPython
#  message spec. The Session will handle serialization/deserialization, security,
#  and metadata.
#  
#  Sessions support configurable serialization via packer/unpacker traits, and
#  signing with HMAC digests via the key/keyfile traits.
#  
#  Parameters ----------
#  
#  debug : bool
#      whether to trigger extra debugging statements
#  packer/unpacker : str : 'json', 'pickle' or import_string
#      importstrings for methods to serialize message parts.  If just
#      'json' or 'pickle', predefined JSON and pickle packers will be used.
#      Otherwise, the entire importstring must be used.
#  
#      The functions must accept at least valid JSON input, and output *bytes*.
#  
#      For example, to use msgpack:
#      packer = 'msgpack.packb', unpacker='msgpack.unpackb'
#  pack/unpack : callables
#      You can also set the pack/unpack callables for serialization directly.
#  session : bytes
#      the ID of this Session object.  The default is to generate a new UUID.
#  username : unicode
#      username added to message headers.  The default is to ask the OS.
#  key : bytes
#      The key used to initialize an HMAC signature.  If unset, messages
#      will not be signed or checked.
#  keyfile : filepath
#      The file containing a key.  If this is set, `key` will be initialized
#      to the contents of the file.

## Threshold (in bytes) beyond which an object's buffer should be extracted to
#  avoid pickling.
#c.Session.buffer_threshold = 1024

## Whether to check PID to protect against calls after fork.
#  
#  This check can be disabled if fork-safety is handled elsewhere.
#c.Session.check_pid = True

## Threshold (in bytes) beyond which a buffer should be sent without copying.
#c.Session.copy_threshold = 65536

## Debug output in the Session
#c.Session.debug = False

## The maximum number of digests to remember.
#  
#  The digest history will be culled when it exceeds this value.
#c.Session.digest_history_size = 65536

## The maximum number of items for a container to be introspected for custom
#  serialization. Containers larger than this are pickled outright.
#c.Session.item_threshold = 64

## execution key, for signing messages.
#c.Session.key = b''

## path to file containing execution key.
#c.Session.keyfile = ''

## Metadata dictionary, which serves as the default top-level metadata dict for
#  each message.
#c.Session.metadata = {}

## The name of the packer for serializing messages. Should be one of 'json',
#  'pickle', or an import name for a custom callable serializer.
#c.Session.packer = 'json'

## The UUID identifying this session.
#c.Session.session = ''

## The digest scheme used to construct the message signatures. Must have the form
#  'hmac-HASH'.
#c.Session.signature_scheme = 'hmac-sha256'

## The name of the unpacker for unserializing messages. Only used with custom
#  functions for `packer`.
#c.Session.unpacker = 'json'

## Username for the Session. Default is your system username.
#c.Session.username = 'username'
