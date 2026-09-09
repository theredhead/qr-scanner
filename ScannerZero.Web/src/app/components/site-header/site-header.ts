import { Component, Input } from '@angular/core';

@Component({
  selector: 'sz-site-header',
  standalone: true,
  templateUrl: './site-header.html',
  styleUrl: './site-header.css'
})
export class SiteHeaderComponent {
  @Input({ required: true }) repositoryUrl = '';
}
