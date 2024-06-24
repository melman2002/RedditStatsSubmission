import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';

interface Stats {
  posts: Array<Post>;
  users: Array<User>;
}

interface Post {
  title: string;
  username: string;
  created: number;
  upVotesCount: number;
  url: string;
}

interface User {
  name: string;
  postsCount: number;
}

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  public stats: Stats | undefined;
  public currentTime: number = Date.now() / 1000;

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.getStats();
  }

  getStats() {

    this.http.get<Stats>('/stats').subscribe(
      (result) => {
        this.stats = result;
        setTimeout(() => this.getStats(), 5);
      },
      (error) => {
        console.error(error);
        setTimeout(() => this.getStats(), 5);
      }
    );
  }

  public getMinutesAgo(created: number) {
    return Math.floor((Date.now() / 1000 - created) / 60);
  }

  title = 'redditstats.client';
}
