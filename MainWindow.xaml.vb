Imports Microsoft.Kinect
Imports System.IO

'------------------------------------------------------------------------------
' <copyright file="MainWindow.xaml.cs" company="Microsoft">
'     Copyright (c) Microsoft Corporation.  All rights reserved.
' </copyright>
'------------------------------------------------------------------------------

Namespace Microsoft.Samples.Kinect.SkeletonBasics

    ''' <summary>
    ''' Interaction logic for MainWindow.xaml
    ''' </summary>
    Partial Public Class MainWindow
        Inherits Window

        ''' <summary>
        ''' Width of output drawing
        ''' </summary>
        Private Const RenderWidth As Single = 640.0F

        ''' <summary>
        ''' Height of our output drawing
        ''' </summary>
        Private Const RenderHeight As Single = 480.0F

        ''' <summary>
        ''' Thickness of drawn joint lines
        ''' </summary>
        Private Const JointThickness As Double = 3

        ''' <summary>
        ''' Thickness of body center ellipse
        ''' </summary>
        Private Const BodyCenterThickness As Double = 10

        ''' <summary>
        ''' Thickness of clip edge rectangles
        ''' </summary>
        Private Const ClipBoundsThickness As Double = 0

        ''' <summary>
        ''' Brush used to draw skeleton center point
        ''' </summary>
        Private ReadOnly centerPointBrush As Brush = Brushes.Blue

        ''' <summary>
        ''' Brush used for drawing joints that are currently tracked
        ''' </summary>
        Private ReadOnly trackedJointBrush As Brush = New SolidColorBrush(Color.FromArgb(255, 68, 192, 68))

        ''' <summary>
        ''' Brush used for drawing joints that are currently inferred
        ''' </summary>        
        Private ReadOnly inferredJointBrush As Brush = Brushes.Yellow

        ''' <summary>
        ''' Pen used for drawing bones that are currently tracked
        ''' </summary>
        Private ReadOnly trackedBonePen As New Pen(Brushes.DarkKhaki, 6)

        ''' <summary>
        ''' Pen used for drawing bones that are currently inferred
        ''' </summary>        
        Private ReadOnly inferredBonePen As New Pen(Brushes.Gray, 1)

        ''' <summary>
        ''' Active Kinect sensor
        ''' </summary>
        Private sensor As KinectSensor

        ''' <summary>
        ''' Drawing group for skeleton rendering output
        ''' </summary>
        Private drawingGroup As DrawingGroup

        ''' <summary>
        ''' Drawing image that we will display
        ''' </summary>
        Private imageSource As DrawingImage

        ''' <summary>
        ''' Initializes a new instance of the MainWindow class.
        ''' </summary>
        Public Sub New()
            InitializeComponent()
        End Sub

        ''' <summary>
        ''' Execute startup tasks
        ''' </summary>
        ''' <param name="sender">object sending the event</param>
        ''' <param name="e">event arguments</param>
        Private Sub WindowLoaded(ByVal sender As Object, ByVal e As RoutedEventArgs)
            ' Create the drawing group we'll use for drawing
            Me.drawingGroup = New DrawingGroup()

            ' Create an image source that we can use in our image control
            Me.imageSource = New DrawingImage(Me.drawingGroup)

            ' Display the drawing using our image control
            Image.Source = Me.imageSource

            ' Look through all sensors and start the first connected one.
            ' This requires that a Kinect is connected at the time of app startup.
            ' To make your app robust against plug/unplug, it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            For Each sensorItem In KinectSensor.KinectSensors
                If sensorItem.Status = KinectStatus.Connected Then
                    Me.sensor = sensorItem
                    Exit For
                End If
            Next sensorItem

            If Nothing IsNot Me.sensor Then
                ' Turn on the skeleton stream to receive skeleton frames
                Me.sensor.SkeletonStream.Enable()

                ' Add an event handler to be called whenever there is new color frame data
                AddHandler Me.sensor.SkeletonFrameReady, AddressOf SensorSkeletonFrameReady

                ' Start the sensor!
                Try
                    Me.sensor.Start()
                Catch e1 As IOException
                    Me.sensor = Nothing
                End Try
            End If

            If Nothing Is Me.sensor Then

            End If
        End Sub

        ''' <summary>
        ''' Execute shutdown tasks
        ''' </summary>
        ''' <param name="sender">object sending the event</param>
        ''' <param name="e">event arguments</param>
        Private Sub WindowClosing(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs)
            If Nothing IsNot Me.sensor Then
                Me.sensor.Stop()
            End If
        End Sub

        ''' <summary>
        ''' Event handler for Kinect sensor's SkeletonFrameReady event
        ''' </summary>
        ''' <param name="sender">object sending the event</param>
        ''' <param name="e">event arguments</param>
        Private Sub SensorSkeletonFrameReady(ByVal sender As Object, ByVal e As SkeletonFrameReadyEventArgs)
            Dim skeletons(-1) As Skeleton

            Using skeletonFrame As SkeletonFrame = e.OpenSkeletonFrame()
                If skeletonFrame IsNot Nothing Then
                    skeletons = New Skeleton(skeletonFrame.SkeletonArrayLength - 1) {}
                    skeletonFrame.CopySkeletonDataTo(skeletons)
                End If
            End Using

            Using dc As DrawingContext = Me.drawingGroup.Open()
                ' Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Green, Nothing, New Rect(0.0, 0.0, RenderWidth, RenderHeight))

                If skeletons.Length <> 0 Then
                    For Each skel As Skeleton In skeletons
                        Me.RenderClippedEdges(skel, dc)

                        If skel.TrackingState = SkeletonTrackingState.Tracked Then
                            Me.DrawBonesAndJoints(skel, dc)
                        ElseIf skel.TrackingState = SkeletonTrackingState.PositionOnly Then
                            dc.DrawEllipse(Me.centerPointBrush, Nothing, Me.SkeletonPointToScreen(skel.Position), BodyCenterThickness, BodyCenterThickness)
                        End If
                    Next skel
                End If
            End Using
        End Sub

        ''' <summary>
        ''' Draws indicators to show which edges are clipping skeleton data
        ''' </summary>
        ''' <param name="skeleton">skeleton to draw clipping information for</param>
        ''' <param name="drawingContext">drawing context to draw to</param>
        Private Sub RenderClippedEdges(ByVal skeleton As Skeleton, ByVal drawingContext As DrawingContext)
            If skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom) Then
                drawingContext.DrawRectangle(Brushes.Red, Nothing, New Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness))
            End If

            If skeleton.ClippedEdges.HasFlag(FrameEdges.Top) Then
                drawingContext.DrawRectangle(Brushes.Red, Nothing, New Rect(0, 0, RenderWidth, ClipBoundsThickness))
            End If

            If skeleton.ClippedEdges.HasFlag(FrameEdges.Left) Then
                drawingContext.DrawRectangle(Brushes.Red, Nothing, New Rect(0, 0, ClipBoundsThickness, RenderHeight))
            End If

            If skeleton.ClippedEdges.HasFlag(FrameEdges.Right) Then
                drawingContext.DrawRectangle(Brushes.Red, Nothing, New Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight))
            End If
        End Sub

        ''' <summary>
        ''' Draws a skeleton's bones and joints
        ''' </summary>
        ''' <param name="skeleton">skeleton to draw</param>
        ''' <param name="drawingContext">drawing context to draw to</param>
        Private Sub DrawBonesAndJoints(ByVal skeleton As Skeleton, ByVal drawingContext As DrawingContext)
            ' Render Torso
            Me.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter)
            Me.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft)
            Me.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight)
            Me.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine)
            Me.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter)
            Me.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft)
            Me.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight)

            ' Left Arm
            Me.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft)
            Me.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft)
            Me.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft)

            ' Right Arm
            Me.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight)
            Me.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight)
            Me.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight)

            ' Left Leg
            Me.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft)
            Me.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft)
            Me.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft)

            ' Right Leg
            Me.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight)
            Me.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight)
            Me.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight)

            ' Render Joints
            For Each joint As Joint In skeleton.Joints
                Dim drawBrush As Brush = Nothing

                If joint.TrackingState = JointTrackingState.Tracked Then
                    drawBrush = Me.trackedJointBrush
                ElseIf joint.TrackingState = JointTrackingState.Inferred Then
                    drawBrush = Me.inferredJointBrush
                End If

                If drawBrush IsNot Nothing Then
                    drawingContext.DrawEllipse(drawBrush, Nothing, Me.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness)
                End If
            Next joint
        End Sub

        ''' <summary>
        ''' Clips a SkeletonPoint to lie within our output space and converts to Point
        ''' </summary>
        ''' <param name="skelpoint">point to convert and clip</param>
        ''' <returns>converted and clipped point</returns>
        Private Function SkeletonPointToScreen(ByVal skelpoint As SkeletonPoint) As Point
            ' Convert point to depth space.  
            ' We are not using depth directly, but we do want the points in our 640x480 output resolution.
            Dim depthPoint As DepthImagePoint = Me.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30)

            ' Restrict to our render space
            Dim x As Double = depthPoint.X
            If x > RenderWidth Then
                x = RenderWidth
            ElseIf x < 0 Then
                x = 0
            End If

            Dim y As Double = depthPoint.Y
            If y > RenderHeight Then
                y = RenderHeight
            ElseIf y < 0 Then
                y = 0
            End If

            Return New Point(x, y)
        End Function

        ''' <summary>
        ''' Draws a bone line between two joints
        ''' </summary>
        ''' <param name="skeleton">skeleton to draw bones from</param>
        ''' <param name="drawingContext">drawing context to draw to</param>
        ''' <param name="jointType0">joint to start drawing from</param>
        ''' <param name="jointType1">joint to end drawing at</param>
        Private Sub DrawBone(ByVal skeleton As Skeleton, ByVal drawingContext As DrawingContext, ByVal jointType0 As JointType, ByVal jointType1 As JointType)
            Dim joint0 As Joint = skeleton.Joints(jointType0)
            Dim joint1 As Joint = skeleton.Joints(jointType1)

            ' If we can't find either of these joints, exit
            If joint0.TrackingState = JointTrackingState.NotTracked OrElse joint1.TrackingState = JointTrackingState.NotTracked Then
                Return
            End If

            ' Don't draw if both points are inferred
            If joint0.TrackingState = JointTrackingState.Inferred AndAlso joint1.TrackingState = JointTrackingState.Inferred Then
                Return
            End If

            ' We assume all drawn bones are inferred unless BOTH joints are tracked
            Dim drawPen As Pen = Me.inferredBonePen
            If joint0.TrackingState = JointTrackingState.Tracked AndAlso joint1.TrackingState = JointTrackingState.Tracked Then
                drawPen = Me.trackedBonePen
            End If

            drawingContext.DrawLine(drawPen, Me.SkeletonPointToScreen(joint0.Position), Me.SkeletonPointToScreen(joint1.Position))
        End Sub

        ''' <summary>
        ''' Handles the checking or unchecking of the seated mode combo box
        ''' </summary>
        ''' <param name="sender">object sending the event</param>
        ''' <param name="e">event arguments</param>
        Private Sub CheckBoxSeatedModeChanged(ByVal sender As Object, ByVal e As RoutedEventArgs)
            If Me.sensor IsNot Nothing Then
                
                    Me.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default

            End If
        End Sub
    End Class
End Namespace