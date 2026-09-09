import { Component, Input } from '@angular/core';

export type IconName = 'arrow-up-right' | 'chevron-left' | 'chevron-right';

@Component({
  selector: 'sz-icon',
  standalone: true,
  templateUrl: './icon.html',
  styleUrl: './icon.css'
})
export class IconComponent {
  @Input({ required: true }) name!: IconName;
  @Input() label = '';
}
