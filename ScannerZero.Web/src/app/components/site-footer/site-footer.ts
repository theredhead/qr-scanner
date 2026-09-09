import { Component, Input } from '@angular/core';

@Component({
  selector: 'sz-site-footer',
  standalone: true,
  templateUrl: './site-footer.html',
  styleUrl: './site-footer.css'
})
export class SiteFooterComponent {
  @Input({ required: true }) repositoryUrl = '';
  @Input({ required: true }) licenseUrl = '';
}
