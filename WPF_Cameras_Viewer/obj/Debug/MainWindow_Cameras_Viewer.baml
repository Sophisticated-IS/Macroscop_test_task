   M S B A M L   `   `   `  ����M  IWPF_Cameras_Viewer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null7 clr-namespace:WPF_Cameras_ViewerWPF_Cameras_Viewer  5   	   2    ,WPF_Cameras_Viewer.MainWindow_Cameras_Viewer   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   2local clr-namespace:WPF_Cameras_Viewer     5   	   -   5          � Title$  Macroscop Cameras Viewer��5   	   $	��450��6*   $	��800��67    � 	Resources 5
       &  "BigFont_TextBlock_camera_inf_Style �� Property �� Value  Play_Mouse_hover_Style �� 
TargetType �� Triggers P� Property P� Value �� BasedOn��%�  &	        &	 �     '��     7   �� 5   
   .�5      ��  � ��$ Times New Roman)�68   �� 5       � ��$
 Bold"�68   �� 5       � ��$ 20(�68   �� 5       � ��$ Italic$�67   �� 5       �@K�ZIndex$ 2��6-   5      �� 5   
   " ��6/   �5      �� 5       � ��# � ��6/   �� 5       � ��$ 0q�64   �� 5       � ��$ 18(�6-   �� 5       � ��$ 	ExtraBold"�6/    5      P� 5       �@J�IsMouseOver . . �5      ��  � �� �	#FEDF983763   5       5#      �� 5&   
   " ��6   8   �5'      �� 5(       � ��$ 0q�64   5*      5,      .��5-      � #��� ��6   	 � ColumnDefinitions	 5.   
   �� 5/      $��4* �6   �� 50      $��1* �6   .+�53   
   � $��0��6   $��Stretch=�6   $��Stretch=�6=   
 � RowDefinitions
 54      �� 55      $��* �6    �� 56      $
��50px��6    $	��100��61   .+�58      � $��0��6   $��Stretch=�6!   $��Stretch=�6?   .+�59      ��  �� Orientation$ 
Horizontal=�6    K� ZIndex$ 2��66   .+�5:      �� -   5        $��txtblock_camera_name��5:       $��Camera's name��6<   #��[  6Q   ���#FFD3006�   �� 5;      -   5        $��txtblock_time��5;       $��28/09/2000 10:30��65   $��10,0,0,0q�6M   #��[  6a   
�������6�   5<      �� 5>      -   5         ��Name$ img_stream_picture��5>      $
��Fill=�64   $��Center=�6D   $��Center=�6a   6   �� 5?      $��1q�6   
��� ���$��Right=�6B   6`   5@      � 5A      $��1��6   ��5B      �� 5C       �� EndPoint$ 0.5,1&�6*    � MappingMode$ RelativeToBoundingBox=�6;    �� 
StartPoint$ 0.5,0&�6_   .��5D      �  � Color$ 	#FFFFBB3F��6'    � Offset$ 1Q�69   � 5E      $ 	#FFD7E4F2��6'   $ 0.268Q�69   � 5F      $ 	#FF52AACA��6'   $
 0.18Q�69   � 5G      $ 	#FFD7E4F2��6'   $ 0.735Q�69   5H      .+�5J      �� $��1q�6   $	��Top=�6-   
��� ���6]   �� 5K      $ Vertical=�6   $��Right=�64   $��Stretch=�6P   $��0,5,0,0q�6l   .+�5L      �� $��Quality:��6    $��14(�60   
���   �$��Medium"�6Q   �� 5M      -   5        $��combox_quality��5M       $��100px��67   $
��50px��6H   $��0,0,5,0q�6Y   $��0q�6j   $
��auto��6~   .@�5N      �� $	��Low��6'   �� 5O      $��Middle��6'   �� 5P      $
��High��6'   5Q      5R      �� 5S      $ 
Horizontal=�6   $��Center=�66   .+�5U      �� -   5        $��button_left_arrow��5U      $��0q�66   #��� ��6J   $
��20px��6�   $
��50px��6�   $
��Left=�6�   ��5V      �� 5W      -   5        $��img_left_arrow��5W   $   5Y      �� 5Z      -   5        $��button_play��5Z      #��[ 61   $��	50,0,50,0q�6a   $
��20px��6t   $	��200��6�   $
��50px��6�   $��Stretch=�6�   $
��Play��6�   ��5[      
� 5\      .��5]   "   � $ 	#FFD7E4F2��6/   $ 0.821Q�6A   � 5^   "   $ White��6/   � 5_   "   $ 	#FEDF9837��6/   $ 0.608Q�6A   5`      5b      �� 5c      -	   5        $��button_right_arrow��5c      $��0q�69   #��� ��6N   $
��20px��6�   $
��50px��6�   ��5d      �� 5e      -
   5        $��img_right_arrow��5e   $   5g      5h      5j      5l      � 5n   
   $��1��6   $��5,0,0,0q�6   
 5o      �� 5p      $
��auto �6    �� 5q      $��* �6    .+�5s      � $��0��6   .+�5t      �� $��Available cameras:��6   #��[  67   $��	UltraBold"�6t   $
��Left=�6�   6�   5u      � 5w      $��1��6   .+�5x      �� -   5        $��list_view_availab_cameras��5x      
���������5y      �� 5z      .R�5{      A� .+�5|   "   �� $ Vertical=�6.   .+�5}   &   �� #��[  6N   
����  ���60   �� *Camera_Name+6�   �� 5~   &   $
��Left=�6,   $	��200��6g   ��6G   �� *Camera_Frame+6g   5   #   5�      5�      5�      5�      5�      5�      5�      