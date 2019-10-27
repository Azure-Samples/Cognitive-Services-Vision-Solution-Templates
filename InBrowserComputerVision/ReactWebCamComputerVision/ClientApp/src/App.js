import React, { Component } from 'react';
import { Route } from 'react-router';
import { WebCamCV } from './components/WebCamCV';
import { Layout } from './components/Layout';

export default class App extends Component {
  static displayName = App.name;

  render () {
    return (
      <Layout>
            <Route exact path='/' component={WebCamCV} />
      </Layout>
    );
  }
}
