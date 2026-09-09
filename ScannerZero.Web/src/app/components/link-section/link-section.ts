import { Component, Input } from '@angular/core';
import { IconComponent } from '../icon/icon';

export type Link = {
  label: string;
  href: string;
  note: string;
};

@Component({
  selector: 'sz-link-section',
  standalone: true,
  imports: [IconComponent],
  templateUrl: './link-section.html',
  styleUrl: './link-section.css'
})
export class LinkSectionComponent {
  @Input({ required: true }) headingId = '';
  @Input({ required: true }) eyebrow = '';
  @Input({ required: true }) title = '';
  @Input({ required: true }) links: Link[] = [];
  @Input() sectionId = '';
  @Input() variant: 'plain' | 'raised' | 'muted' = 'plain';
}
