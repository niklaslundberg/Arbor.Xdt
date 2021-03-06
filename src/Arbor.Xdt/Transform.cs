using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Xml;

namespace Arbor.Xdt
{
    public abstract class Transform
    {
        protected Transform()
            : this(TransformFlags.None)
        {
        }

        protected Transform(TransformFlags flags)
            : this(flags, MissingTargetMessage.Warning)
        {
        }

        protected Transform(TransformFlags flags, MissingTargetMessage message)
        {
            MissingTargetMessage = message;
            ApplyTransformToAllTargetNodes = (flags & TransformFlags.ApplyTransformToAllTargetNodes)
                                             == TransformFlags.ApplyTransformToAllTargetNodes;
            UseParentAsTargetNode =
                (flags & TransformFlags.UseParentAsTargetNode) == TransformFlags.UseParentAsTargetNode;
        }

        protected bool ApplyTransformToAllTargetNodes { get; set; }

        protected bool UseParentAsTargetNode { get; set; }

        protected MissingTargetMessage MissingTargetMessage { get; set; }

        protected XmlNode TransformNode => _currentTransformNode ?? _context.TransformNode;

        protected XmlNode TargetNode
        {
            get
            {
                if (_currentTargetNode == null)
                {
                    foreach (XmlNode targetNode in TargetNodes)
                    {
                        return targetNode;
                    }
                }

                return _currentTargetNode;
            }
        }

        protected XmlNodeList TargetNodes
        {
            get
            {
                if (UseParentAsTargetNode)
                {
                    return _context.TargetParents;
                }

                return _context.TargetNodes;
            }
        }

        protected XmlNodeList TargetChildNodes => _context.TargetNodes;

        protected XmlTransformationLogger Log
        {
            get
            {
                if (_logger == null)
                {
                    _logger = _context.GetService<XmlTransformationLogger>();

                    if (_logger != null)
                    {
                        _logger.CurrentReferenceNode = _context.TransformAttribute;
                    }
                }

                return _logger;
            }
        }

        protected string ArgumentString { get; private set; }

        protected IList<string> Arguments
        {
            get
            {
                if (_arguments == null && ArgumentString != null)
                {
                    _arguments = XmlArgumentUtility.SplitArguments(ArgumentString);
                }

                return _arguments;
            }
        }

        private string TransformNameLong
        {
            get
            {
                if (_context.HasLineInfo)
                {
                    return string.Format(CultureInfo.CurrentCulture,
                        SR.XMLTRANSFORMATION_TransformNameFormatLong,
                        TransformName,
                        _context.TransformLineNumber,
                        _context.TransformLinePosition);
                }

                return TransformNameShort;
            }
        }

        internal string TransformNameShort => string.Format(CultureInfo.CurrentCulture,
            SR.XMLTRANSFORMATION_TransformNameFormatShort,
            TransformName);

        private string TransformName => GetType().Name;

        protected abstract void Apply();

        protected T GetService<T>() where T : class
        {
            return _context.GetService<T>();
        }

        private void ReleaseLogger()
        {
            if (_logger != null)
            {
                _logger.CurrentReferenceNode = null;
                _logger = null;
            }
        }

        private bool ApplyOnAllTargetNodes()
        {
            bool error = false;
            XmlNode originalTransformNode = TransformNode;

            foreach (XmlNode node in TargetNodes)
            {
                try
                {
                    _currentTargetNode = node;
                    _currentTransformNode = originalTransformNode.Clone();

                    ApplyOnce();
                }
                catch (Exception ex)
                {
                    Log.LogErrorFromException(ex);
                    error = true;
                }
            }

            _currentTargetNode = null;

            return error;
        }

        private void ApplyOnce()
        {
            WriteApplyMessage(TargetNode);
            Apply();
        }

        private void WriteApplyMessage(XmlNode targetNode)
        {
            if (targetNode is IXmlLineInfo lineInfo)
            {
                Log.LogMessage(MessageType.Verbose,
                    SR.XMLTRANSFORMATION_TransformStatusApplyTarget,
                    targetNode.Name,
                    lineInfo.LineNumber,
                    lineInfo.LinePosition);
            }
            else
            {
                Log.LogMessage(MessageType.Verbose,
                    SR.XMLTRANSFORMATION_TransformStatusApplyTargetNoLineInfo,
                    targetNode.Name);
            }
        }

        private bool ShouldExecuteTransform()
        {
            return HasRequiredTarget();
        }

        private bool HasRequiredTarget()
        {
            bool hasRequiredTarget;
            bool existedInOriginal;
            XmlElementContext matchFailureContext;

            if (UseParentAsTargetNode)
            {
                hasRequiredTarget = _context.HasTargetParent(out matchFailureContext, out existedInOriginal);
            }
            else
            {
                hasRequiredTarget = _context.HasTargetNode(out matchFailureContext, out existedInOriginal);
            }

            if (!hasRequiredTarget)
            {
                HandleMissingTarget(matchFailureContext, existedInOriginal);
                return false;
            }

            return true;
        }

        private void HandleMissingTarget(XmlElementContext matchFailureContext, bool existedInOriginal)
        {
            string messageFormat = existedInOriginal
                ? SR.XMLTRANSFORMATION_TransformSourceMatchWasRemoved
                : SR.XMLTRANSFORMATION_TransformNoMatchingTargetNodes;

            string message = string.Format(CultureInfo.CurrentCulture,
                messageFormat,
                matchFailureContext.XPath);
            switch (MissingTargetMessage)
            {
                case MissingTargetMessage.None:
                    Log.LogMessage(MessageType.Verbose, message);
                    break;
                case MissingTargetMessage.Information:
                    Log.LogMessage(MessageType.Normal, message);
                    break;
                case MissingTargetMessage.Warning:
                    Log.LogWarning(matchFailureContext.Node, message);
                    break;
                case MissingTargetMessage.Error:
                    throw new XmlNodeException(message, matchFailureContext.Node);
            }
        }

        internal void Execute(XmlElementContext context, string argumentString)
        {
            Debug.Assert(_context == null && ArgumentString == null, "Don't call Execute recursively");
            Debug.Assert(_logger == null, "Logger wasn't released from previous execution");

            if (_context == null && ArgumentString == null)
            {
                bool error = false;
                bool startedSection = false;

                try
                {
                    _context = context;
                    ArgumentString = argumentString;
                    _arguments = null;

                    if (ShouldExecuteTransform())
                    {
                        startedSection = true;

                        Log.StartSection(MessageType.Verbose,
                            SR.XMLTRANSFORMATION_TransformBeginExecutingMessage,
                            TransformNameLong);
                        Log.LogMessage(MessageType.Verbose, SR.XMLTRANSFORMATION_TransformStatusXPath, context.XPath);

                        if (ApplyTransformToAllTargetNodes)
                        {
                            ApplyOnAllTargetNodes();
                        }
                        else
                        {
                            ApplyOnce();
                        }
                    }
                }
                catch (Exception ex)
                {
                    error = true;

                    if (context.TransformAttribute != null)
                    {
                        Log.LogErrorFromException(XmlNodeException.Wrap(ex, context.TransformAttribute));
                    }
                    else
                    {
                        Log.LogErrorFromException(ex);
                    }
                }
                finally
                {
                    if (startedSection)
                    {
                        if (error)
                        {
                            Log.EndSection(MessageType.Verbose,
                                SR.XMLTRANSFORMATION_TransformErrorExecutingMessage,
                                TransformNameShort);
                        }
                        else
                        {
                            Log.EndSection(MessageType.Verbose,
                                SR.XMLTRANSFORMATION_TransformEndExecutingMessage,
                                TransformNameShort);
                        }
                    }
                    else
                    {
                        Log.LogMessage(MessageType.Normal,
                            SR.XMLTRANSFORMATION_TransformNotExecutingMessage,
                            TransformNameLong);
                    }

                    _context = null;
                    ArgumentString = null;
                    _arguments = null;

                    ReleaseLogger();
                }
            }
        }

        #region private data members

        private XmlTransformationLogger _logger;
        private XmlElementContext _context;
        private XmlNode _currentTransformNode;
        private XmlNode _currentTargetNode;

        private IList<string> _arguments;

        #endregion
    }
}