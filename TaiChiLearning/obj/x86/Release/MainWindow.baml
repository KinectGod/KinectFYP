   M S B A M L   `   `   `  ����   GrabSkeletonData!    GrabSkeletonData.MainWindow   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     -     � Title$  	KinectDTW��$	��720��$
��1000�� � 	Resources    MediumGreyBrush �� Color  KinectBlueBrush  KinectPurpleBrush �� 
TargetType  ResetButton �� Property �� Value �� 
TargetType �� Orientation ��Name	 ��Name
 �� TextAlignment �� Triggers P� Property P� Value �� 
TargetName  
MeshButton  KinectRadioButton �Name  KinectCheckBox  SliderRepeatButtonStyle  HorizontalSliderThumbStyle J� SnapsToDevicePixels ��Name 	 KinectSlider � RowDefinitions m�Name m� Fill m� 	Placement �� CornerRadius ��Name ]�Name ]� DecreaseRepeatButton �� DecreaseLarge ]� IncreaseRepeatButton �� IncreaseLarge ]� Thumb n�Name 
 KinectTextFormat��%B  &	        &	      &	 *     &	 ?     7  7  &	 0    7  7  &	     7  7  &	 -    7  7  &	 #    &	 �    7  &		 �    7  7  7  7  7  7  &	
 %    �� $ 	#ff6e6e6e���� $ 	#ff00BCF2���� $ 	#ff52318f���� " ��.���  � �� �� " ��.R��� $ 
Horizontal=�
������ .+�� .+��� $ ResetNormal��$��Images\ResetNormal.png��$
��None=�$��Center=��� $ 
ResetHover��$��Images\ResetHover.png��$
��None=�$��Center=�$y�	Collapsed=��� $	 	ResetText��#��z��$

 Left=�$��Center=�8��  $��15(�$��5 0 0 0q� P�  �@J�IsMouseOver . . ��� $ ResetNormal�� � y�$ 	Collapsed=��� $ 
ResetHover�� � y�$ Visible=��� 8  $ 	ResetText�� � ���� " ��.���  � �� �� " ��.R��� $ 
Horizontal=�
������ .+�� .+��� $ 
MeshNormal��$��Images\MeshNormal.png��$
��None=�$��Center=��� $ 	MeshHover��$��Images\MeshHover.png��$
��None=�$��Center=�$y�	Collapsed=��� $	 MeshText��#��z��$

 Left=�$��Center=�8��  $��15(�$��5 0 0 0q� P�  �@J�IsMouseOver . . ��� $ 
MeshNormal�� � y�$ 	Collapsed=��� $ 	MeshHover�� � y�$ Visible=��� 8  $ MeshText�� � ���� " 	�.���  � �� �� " 	�.R��� $ 
Horizontal=�
������ .+�� $ RadioButtonChecked��.+��� $ CheckedNormal��$#��Images\RadioCheckedNormal.png��$
��None=�$��Center=��� $ CheckedHover��$"��Images\RadioCheckedHover.png��$
��None=�$��Center=�$y�	Collapsed=�� $ RadioButtonUnchecked��$y�	Collapsed=�.+��� $ UncheckedNormal��$%��Images\RadioUncheckedNormal.png��$
��None=�$��Center=��� $ UncheckedHover��$$��Images\RadioUncheckedHover.png��$
��None=�$��Center=�$y�	Collapsed=��� $	 RadioButtonText��#��z��$

 Left=�$��Center=�8��  $��15(�$��5 0 0 0q� P�  �@d�	IsChecked$ falseY�. ��� $ RadioButtonChecked�� � y�$ 	Collapsed=��� $ RadioButtonUnchecked�� � y�$ Visible=�P�  �@J�IsMouseOver . . ��� $ CheckedNormal�� � y�$ 	Collapsed=��� $ UncheckedNormal�� � y�$ 	Collapsed=��� $ CheckedHover�� � y�$ Visible=��� $ UncheckedHover�� � y�$ Visible=��� 8  $ RadioButtonText�� � ���� " ��.���  � �� �� " ��.R�� .+��� $ 
Horizontal=�
������ .+�� $ CheckBoxChecked��.+��� $ CheckedNormal��$��Images\CheckedNormal.png��$
��None=�$��Center=��� $ CheckedHover��$��Images\CheckedHover.png��$
��None=�$��Center=�$y�	Collapsed=�� $ CheckBoxUnchecked��$y�	Collapsed=�.+��� $ UncheckedNormal��$ ��Images\UncheckedNormal.png��$
��None=�$��Center=��� $ UncheckedHover��$��Images\UncheckedHover.png��$
��None=�$��Center=�$y�	Collapsed=��� $	 CheckBoxText��#��z��$

 Left=�$��Center=�8��  $��15(�$��9,0,0,0q� P�  �@d�	IsChecked$ falseY�. ��� $ CheckBoxChecked�� � y�$ 	Collapsed=��� $ CheckBoxUnchecked�� � y�$ Visible=�P�  �@J�IsMouseOver . . ��� $ CheckedNormal�� � y�$ 	Collapsed=��� $ UncheckedNormal�� � y�$ 	Collapsed=��� $ CheckedHover�� � y�$ Visible=��� $ UncheckedHover�� � y�$ Visible=��� 8  $ CheckBoxText�� � ���� " ��.���  �@�OverridesDefaultStyle . ��  � �� .  ��  � |� .  ��  � �� �� " ��.R��� 
������ �� " n�.���  � |� .  ��  �@�OverridesDefaultStyle . ��  � ��$ 22����  � ��$ 11����  � ��8   ��  � �� �� " n�.R���  . .+��� $
 Body��$��11��$��22��#��z���� " ��.���  �@��IsPressAndHoldEnabled .  ��  � ��
 ���� ��  � ��8   ��  � �� �� " ��.R��� #��z��#��z��#��z�� . ._��  �� $
��Auto ��� $
��Auto �#��z���� $
��Auto �.+�m� $
 Tick��# z��$��4��$	 Top=�$��0��$y�Visible=��� $ TrackBackground��#��z��$ 1��$	��4.0��$	��5,0q�$��1��$��center=�._��� $��-6,-1q�.+��� $ PART_SelectionRange��#��� ��$	��4.0��#��� ��$	��1.0��$y�Hidden=�]� $ 
PART_Track��$��1�� �� #��Z 8��  �� #��Z 8��  n� $ Thumb��8��  P�  �@J�IsMouseOver . . ��� 8   � ���� 8  $ Thumb�� � ���� " ��.���  � ��$ Center=���  � ��$ Center=���  � ��$ Segoe UI)���  � ��$ 15(�.��� $��	10 0 10 0q� �� $
��Auto ��� $��* ��� $
��Auto ��� $
��Auto �.+�]� $��0��$��0 0 0 20q�.+��� $
��Left=�$��0 10 0 5q�$
��Fill=�$��32��$��81��$��Images\Logo.png���� $��Right=�$��Bottom=�$��Segoe UI)�$��Center=�$��18(�#��[  Happy Taichi Learning� $��1�� � ColumnDefinitions �� $��1* ��� $��1* �.+�� $��0��.+��� .+��� -   $ ReplayImage��$	��480��$	��360��$	��0,0q�  J� RenderTransformOrigin$  0.5,0.5&����� *Bitmap+�� .+��� -   ! ��Name$! LearningSkeletonCanvas��$	��480��$	��360��}�. $��0,0,0,0q�$  0.5,0.5&�.+��� 
������ " J� IsHitTestVisible" . � $��1��.+��� .+��� -   $ RealTimeImage��$	��480��$	��360��$��0,0,0,50q�$  0.5,0.5&����� *Bitmap+�� .+��� -   $! RealTimeSkeletonCanvas��$	��480��$	��360��}�. $��0,0,0,0q�$  0.5,0.5&�.+��� 
������ " . � $��2��$	��245��$	��Top=� �� $
��Auto ��� $
��Auto ��� $��* �.+��� $��0��#��[ $��2q�._��  �� $
��Auto ��� $��* �.+��� $��0��$��2��#��[ $��
-1 -1 -1 0q�.+��� #��[
 
�������	Actions�� $��1��.+��� -   # ��Name$# gestureList��$	��200��.@��� $ �� 
IsSelected$ . $��
@1stMotion���� $��
@2ndMotion���� $��
@3rdMotion���� $��
@4thMotion���� -   % ��Name$% 
dtwCapture��$��0 0 0 0q�#��[ 
�������$��2q�$��25��$	��186��
�������|�.  $��Record Motion���� -   $% dtwStore��$��0 0 0 0q�#��[ 
�������$��2q�$��25��$	��186��
�������|�.  $��Stop and Store��{�.  �� -	   $% 	dtwReplay��$��0 0 0 0q�#��[ 
�������$��2q�$��25��$	��186��
�������|�.  $��Replay��{�. �� -
   $% dtwStopReplay��$��0 0 0 0q�#��[ 
�������$��2q�$��25��$	��186��
�������|�.  $��Stop Replay��{�.  �� -   $% dtwStartRegcon��$��0 0 0 0q�#��[ 
�������$��2q�$��25��$	��186��
�������|�.  $��Start Learning��{�. �� -   $% dtwStopRegcon��$��0 0 0 0q�#��[ 
�������$��2q�$��25��$	��186��
�������|�.  $��Stop Learning��{�.  �� $��1��#��[ $��2q�$��-1 0 0 0q�$	��400��._��  �� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto � �� $��* ��� $��* �.+��� $��0��$��2��#��[ $��
-1 -1 -1 0q�.+��� #��[
 
�������
Settings�� $��1��$��2��$��10 10 10 10q�.+��� -   $��SpeechRecogn��$��1��#��[ $
��Left=�$��Center=�$��	10 0 10 0q�$��Speech Recognize��4  .System.Windows.Automation.AutomationProperties&   AutomationId$& NearMode��' d� 	IsChecked' �� *
NearMode+�� -   #��[ $��Choose Mode��$��	10 10 5 0q�$& ModeSelection���� $ 
Horizontal=�$��0 -3 0 0q�.+�	� -   $��traning��#��[ $��Training��$��30 5 0 0q�$
' TrueY�$& 	STLFormat��	� -   $��	challenge��#��[ $��	Challenge��$��5 5 0 0q�$& 	OBJFormat���� -   #��[ $��Choose Difficulty��$��	10 10 5 0q�$& DifficultySelection���� $ 
Horizontal=�$��0 -3 0 0q�.+�	� -   $
��easy��#��[ $
��Easy��$��30 5 0 0q�$
' TrueY�$
& Easy��	� -   $��medium��#��[ $��Medium��$��5 5 0 0q�$& Medium��	� -   $
��hard��#��[ $
��Hard��$��5 5 0 0q�$
& Hard��� $��2��$��2�� �� $
��Auto ��� $��* ��� $
��Auto �.+��� $��0��#��[
 #��[ $��5 0 0 0q�
Replay FPS�� $��1��#��[	 ( � SmallChange$
( 0.02Q�) � LargeChange$	) 1.5Q�* � Minimum$
* 0.35Q�+ � Maximum$	+ 8.0Q�, �� Ticks$2, ,0.35, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0R�$��5q�$& DepthThresholdMax��- � Value- �� *MaxDepthClip+� $��2��.+�(� .���� -   $! MasterSkeletonCanvas��$	��152��$	��100��}�. $��0,0,0,0q�$��Right=�$	��Top=����� . �� EndPoint$. 0.5,1&�/ �� 
StartPoint$/ 0.5,0&�.��� 0 � Color$0 	#D9040833��1 � Offset$1 0.238Q�� $0 	#FF46BC44��$1 1Q�� $0 	#E61747B2��$1 0.811Q�� $��3��$��20��$	��Top=�.+��� -   2 ��Name$2 status��$��14(�$��0,0,0,0q�|�.  $��0q�$�� ��3 �� TextAlignment$
3 Left=�$
��Left=�$	��Top=�$��20��